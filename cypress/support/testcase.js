import { VTEX_AUTH_HEADER } from './common/constants.js'
import { updateRetry, saveOrderId } from './common/support.js'
import { invoiceAPI, transactionAPI } from './common/apis.js'
import selectors from './common/selectors.js'
import { getTestVariables } from './utils.js'
import { externalSeller } from './outputvalidation.js'

export function orderTaxAPITestCase(fixtureName, tax) {
  // Verify tax amounts via order-tax API
  it(`Verify ${fixtureName} tax via order-tax API`, { retries: 0 }, () => {
    // Load fixtures request payload and use them in orderTax API
    cy.fixture(fixtureName).then(requestPayload =>
      cy.orderTaxApi(requestPayload, tax)
    )
  })
}

export function completePayment({
  prefix,
  orderIdEnv = false,
  externalSellerEnv = false,
  payerAuth = false,
}) {
  it(`In ${prefix} - Completing the Payment & save OrderId`, () => {
    cy.intercept('**/gatewayCallback/**').as('callback')

    // Select Credit Card Option
    cy.get('a[id*=creditCard]').should('be.visible').click()

    cy.get('body').then($body => {
      if ($body.find(selectors.CreditCard).length) {
        cy.get(selectors.CreditCard).click()
      }
    })

    cy.getIframeBody(selectors.PaymentMethodIFrame)
      .find(selectors.CreditCardCode)
      .should('be.visible')

    cy.getIframeBody(selectors.PaymentMethodIFrame).then($body => {
      if (!$body.find(selectors.CardExist).length) {
        // Credit cart not exist
        cy.getIframeBody(selectors.PaymentMethodIFrame)
          .find(selectors.CreditCardNumber)
          .type('5555 5555 5555 4444')
        cy.getIframeBody(selectors.PaymentMethodIFrame)
          .find(selectors.CreditCardHolderName)
          .type('Syed')
        cy.getIframeBody(selectors.PaymentMethodIFrame)
          .find(selectors.CreditCardExpirationMonth)
          .select('01')
        cy.getIframeBody(selectors.PaymentMethodIFrame)
          .find(selectors.CreditCardExpirationYear)
          .select('30')
      }

      cy.getIframeBody(selectors.PaymentMethodIFrame).then($paymentBtn => {
        if ($paymentBtn.find(selectors.PaymentMethodIFrame).length) {
          cy.getIframeBody(selectors.PaymentMethodIFrame)
            .find('.SavedCard span[class*=Master]')
            .click()
        }
      })

      cy.getIframeBody(selectors.PaymentMethodIFrame)
        .find(selectors.CreditCardCode)
        .type('123')
      cy.get(selectors.BuyNowBtn).last().click()
      cy.wait('@callback', { timeout: 40000 })
        .its('response.statusCode', { timeout: 5000 })
        .should('eq', payerAuth ? 428 : 204)
      saveOrderId(orderIdEnv, externalSellerEnv)
    })
  })
}

function verifyPaymentStarted(interactionResponse) {
  const expectedStatus = 'Payment Started'
  const expectedMessage = 'Authorization response parsed'

  const index = interactionResponse.body.findIndex(
    ob => ob.Status === expectedStatus && ob.Message.includes(expectedMessage)
  )

  if (index >= 0) {
    const jsonString = interactionResponse.body[index].Message.split(': ')

    if (jsonString) {
      const json = JSON.parse(jsonString[1])

      expect(json.status).to.match(/approved|undefined/i)
      expect(json.message).to.match(/authorized|review|null/i)
    }
  } else {
    throw new Error(
      `Unable to find expected Status: ${expectedStatus} and Message: ${expectedMessage} in transaction/interactions response`
    )
  }
}

export function verifyPaymentSettled(prefix, orderIdEnv) {
  const { transactionIdEnv, paymentTransactionIdEnv } = getTestVariables(prefix)

  it(`In ${prefix} - Verify payment settlements`, updateRetry(3), () => {
    cy.addDelayBetweenRetries(5000)
    if (cy.state('runnable')._currentRetry > 0) {
      // Approving Payment via /cybersource/notify API
      approvePayment(orderIdEnv, paymentTransactionIdEnv)
    }

    cy.getOrderItems().then(order => {
      if (order[transactionIdEnv]) {
        cy.getVtexItems().then(vtex => {
          cy.getAPI(
            `${transactionAPI(vtex.baseUrl)}/${
              order[transactionIdEnv]
            }/interactions`,
            VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
          ).then(response => {
            expect(response.status).to.equal(200)
            const expectedStatus = /Transaction Settled/i
            const expectedMessage = /Settled/i
            const index = response.body.findIndex(
              ob =>
                expectedStatus.test(ob.Status) &&
                expectedMessage.test(ob.Message)
            )

            expect(index).to.not.equal(-1)
          })
        })
      } else {
        cy.log(
          `${prefix} order was not successfull / transactionId is not captured`
        )
      }
    })
  })
}

export function verifyStatusInInteractionAPI({
  prefix,
  transactionIdEnv,
  orderIdEnv,
  paymentTransactionIdEnv,
  approved,
}) {
  it(
    `In ${prefix} - Verifying status & message in transaction/interaction API`,
    updateRetry(3),
    () => {
      if (!approved) {
        // Approving Payment via /cybersource/notify API
        approvePayment(orderIdEnv, paymentTransactionIdEnv)
      }

      cy.getVtexItems().then(vtex => {
        cy.getOrderItems().then(item => {
          cy.getAPI(
            `${invoiceAPI(vtex.baseUrl)}/${item[orderIdEnv]}`,
            VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
          ).then(response => {
            expect(response.status).to.equal(200)
            const [{ transactionId }] = response.body.paymentData.transactions

            cy.setOrderItem(transactionIdEnv, transactionId)
            cy.getAPI(
              `${transactionAPI(vtex.baseUrl)}/${transactionId}/interactions`,
              VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
            ).then(interactionResponse => {
              expect(interactionResponse.status).to.equal(200)
              verifyPaymentStarted(interactionResponse)
            })
          })
        })
      })
    }
  )
}

export function verifyAntiFraud({
  prefix,
  transactionIdEnv,
  paymentTransactionIdEnv,
}) {
  it(`In ${prefix} - Verifying AntiFraud status`, () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        cy.getAPI(
          `${vtex.baseUrl}/cybersource-fraud/payment-provider/transactions/${order[transactionIdEnv]}`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(response => {
          expect(response.status).to.equal(200)
          expect(response.body.tid).to.equal(order[paymentTransactionIdEnv])
          expect(response.body.status).to.match(/approved|undefined|denied/i)
        })
      })
    })
  })
}

function callCybersourceAPI(orderId) {
  return cy.getVtexItems().then(vtex => {
    cy.task('cybersourceAPI', {
      vtex,
      tid: orderId,
    })
  })
}

export function verifyRefundTid({
  prefix,
  paymentTransactionIdEnv,
  payerAuth,
}) {
  it(
    `In ${prefix} - Verifying refundtid is created in cybersource API`,
    updateRetry(5),
    () => {
      cy.addDelayBetweenRetries(5000)
      cy.getOrderItems().then(order => {
        callCybersourceAPI(order[paymentTransactionIdEnv]).then(response => {
          expect(response.status).to.equal(200)

          // If payer auth is enabled we will get two relatedTransactions
          // If payer auth is disabled we will get one relatedTransactions

          if (payerAuth) {
            expect(response.data._links.relatedTransactions).to.have.lengthOf(2)
            callCybersourceAPI(
              response.data._links.relatedTransactions[0].href.split('/').at(-1)
            ).then(({ status, data }) => {
              expect(status).to.equal(200)
              expect(data.applicationInformation.applications).to.have.lengthOf(
                1
              )
              expect(
                data.applicationInformation.applications[0].name
              ).to.be.equal('ics_bill')
              expect(
                data.applicationInformation.applications[0].status
              ).to.be.equal('PENDING')
            })
          } else {
            expect(response.data._links.relatedTransactions).to.have.lengthOf(1)
          }

          const refundTransactionId = response.data._links.relatedTransactions[
            payerAuth ? 1 : 0
          ].href
            .split('/')
            .at(-1)

          callCybersourceAPI(refundTransactionId).then(({ status, data }) => {
            expect(status).to.equal(200)
            expect(data.applicationInformation.applications).to.have.lengthOf(2)
            expect(
              data.applicationInformation.applications[0].name
            ).to.be.equal('ics_credit')
            expect(
              data.applicationInformation.applications[0].rMessage
            ).to.be.equal('Request was processed successfully.')
            expect(
              data.applicationInformation.applications[1].name
            ).to.be.equal('ics_credit_auth')
            expect(
              data.applicationInformation.applications[1].rMessage
            ).to.be.equal('Request was processed successfully.')
          })
        })
      })
    }
  )
}

export function verifyCyberSourceAPI({
  prefix,
  transactionIdEnv,
  paymentTransactionIdEnv,
  approved = false,
}) {
  it(`In ${prefix} - Verifying cybersource API`, updateRetry(3), () => {
    cy.addDelayBetweenRetries(5000)
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(item => {
        cy.getAPI(
          `${transactionAPI(vtex.baseUrl)}/${item[transactionIdEnv]}/payments`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(paymentResponse => {
          expect(paymentResponse.body[0].tid).to.be.not.null
          cy.setOrderItem(paymentTransactionIdEnv, paymentResponse.body[0].tid)

          cy.task('cybersourceAPI', {
            vtex,
            tid: paymentResponse.body[0].tid,
            approved,
          }).then(({ status, data }) => {
            expect(status).to.equal(200)
            if (approved) {
              expect(data.applicationInformation.reasonCode).to.equal('100')
              expect(data.riskInformation.profile.decision).to.equal('ACCEPT')
            } else {
              expect(data.applicationInformation.reasonCode).to.equal('480')
              expect(data.riskInformation.profile.decision).to.equal('REVIEW')
            }
          })
        })
      })
    })
  })
}

export function sendInvoiceTestCase({ prefix, totalAmount }, orderIdEnv) {
  it(`In ${prefix} - Send Invoice`, () => {
    cy.getOrderItems().then(item => {
      cy.sendInvoiceAPI(
        {
          invoiceNumber: '54321',
          invoiceValue: totalAmount
            .replace('$ ', '')
            .replace(/\./, '')
            .replace(/,/, ''),
          invoiceUrl: null,
          issuanceDate: new Date(),
          invoiceKey: null,
        },
        item[orderIdEnv],
        orderIdEnv === externalSeller.externalSaleEnv
      ).then(response => {
        expect(response.status).to.equal(200)
      })
    })
  })
}

export function invoiceAPITestCase(
  { prefix, tax: productTax },
  { approved, orderIdEnv, transactionIdEnv },
  { externalSellerTestCase = false } = {}
) {
  let tax

  it(
    `In ${prefix} - Invoice API should have expected tax`,
    updateRetry(6),
    () => {
      cy.addDelayBetweenRetries(5000)
      if (externalSellerTestCase) {
        if (externalSeller.directSaleEnv === orderIdEnv) {
          tax = externalSeller.directSaleTax
        } else {
          tax = externalSeller.externalSellerTax
        }
      }
      // If this is not externalSellerTestCase then it is for refund test case
      else {
        tax = productTax
      }

      cy.getVtexItems().then(vtex => {
        cy.getOrderItems().then(item => {
          cy.getAPI(
            `${
              orderIdEnv === externalSeller.externalSaleEnv
                ? invoiceAPI(vtex.urlExternalSeller)
                : invoiceAPI(vtex.baseUrl)
            }/${item[orderIdEnv]}`,
            VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
          ).then(response => {
            expect(response.status).to.equal(200)
            const [{ transactionId }] = response.body.paymentData.transactions

            cy.setOrderItem(transactionIdEnv, transactionId)
            if (approved) {
              expect(response.body.status).to.match(/cancel|invoiced|handling/)
            } else {
              expect(response.body.status).to.match(/pending|handling|cancel/)
            }

            const taxesArray = response.body.totals.filter(
              el => el.id === 'CustomTax'
            )

            expect(
              taxesArray.reduce((n, { value }) => n + value / 100, 0).toFixed(2)
            ).to.eq(parseFloat(tax.replace('$ ', '')).toFixed(2))
          })
        })
      })
    }
  )
}

function pad(d) {
  return d.toString().padStart(2, '0')
}

function generateXML(orderId, paymentTransactionId) {
  const date = new Date()

  const formattedDate = `${date.getUTCFullYear()}-${pad(
    date.getUTCMonth() + 1
  )}-${pad(date.getUTCDate())} ${pad(date.getUTCHours())}:${pad(
    date.getUTCMinutes()
  )}:${pad(date.getUTCSeconds())}`

  return `content=<?xml version="1.0" encoding="UTF-8"?>
   <!DOCTYPE CaseManagementOrderStatus SYSTEM "https://ebctest.cybersource.com/ebctest/reports/dtd/cmorderstatus_1_1.dtd">
   <CaseManagementOrderStatus xmlns="http://reports.cybersource.com/reports/cmos/1.0" MerchantID="vtex_dev" Name="Case Management Order Status" Date="${formattedDate} GMT" Version="1.1">
     <Update MerchantReferenceNumber="${
       orderId.split('-')[0]
     }" RequestID="${paymentTransactionId}">
       <OriginalDecision>REVIEW</OriginalDecision>
       <NewDecision>ACCEPT</NewDecision>
       <Reviewer>brian</Reviewer>
       <Notes>
         <Note Date="${formattedDate}" AddedBy="brian" Comment="Took ownership." />
       </Notes>
       <Queue>Example</Queue>
       <Profile>Testing</Profile>
     </Update>
   </CaseManagementOrderStatus>`
}

function approvePayment(orderIdEnv, paymentTransactionIdEnv) {
  // Approving Payment via /cybersource/notify API
  cy.getOrderItems().then(item => {
    cy.getVtexItems().then(vtex => {
      cy.request({
        url: `${vtex.baseUrl}/cybersource/notify`,
        method: 'POST',
        body: generateXML(item[orderIdEnv], item[paymentTransactionIdEnv]),
        timeout: 10000,
      }).then(response => {
        expect(response.status).to.equal(200)
      })
    })
  })
}

export function paymentTestCases(
  product,
  { prefix, approved, payerAuth },
  { transactionIdEnv, orderIdEnv }
) {
  if (product) {
    completePayment({ prefix, orderIdEnv, payerAuth })

    invoiceAPITestCase(product, { orderIdEnv, transactionIdEnv, approved })
  }
}

export function APITestCases(
  { prefix, approved },
  { transactionIdEnv, paymentTransactionIdEnv, orderIdEnv }
) {
  if (approved) {
    verifyCyberSourceAPI({
      prefix,
      transactionIdEnv,
      paymentTransactionIdEnv,
      approved,
    })

    verifyStatusInInteractionAPI({
      prefix,
      transactionIdEnv,
      orderIdEnv,
      paymentTransactionIdEnv,
      approved,
    })
  }
}
