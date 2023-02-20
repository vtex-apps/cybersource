import { VTEX_AUTH_HEADER } from './common/constants.js'
import { updateRetry, saveOrderId } from './common/support.js'
import { invoiceAPI, transactionAPI } from './common/apis.js'
import selectors from './common/selectors.js'
import { getTestVariables } from './utils.js'
import { externalSeller } from './outputvalidation.js'
import { ORDER_SUFFIX } from './appSettings.js'

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

function icsBillValidation(applications) {
  expect(applications).to.have.lengthOf(1)
  expect(applications[0].name).to.be.equal('ics_bill')
  // expect(applications[0].status).to.be.equal('PENDING')
}

function icsCreditValidation(applications) {
  expect(applications).to.have.lengthOf(2)
  expect(applications[0].name).to.be.equal('ics_credit')
  expect(applications[0].rMessage).to.be.equal(
    'Request was processed successfully.'
  )
  expect(applications[1].name).to.be.equal('ics_credit_auth')
  expect(applications[1].rMessage).to.be.equal(
    'Request was processed successfully.'
  )
}

function verifyCreditorBill({ payerAuth, transactionId, icsBillAtZeroIndex }) {
  callCybersourceAPI(transactionId).then(({ status, data }) => {
    cy.log(`Using this transactionId - ${transactionId}`)
    expect(status).to.equal(200)
    const { applications } = data.applicationInformation

    if (payerAuth) {
      if (Cypress.env(icsBillAtZeroIndex)) {
        // if icsBill is at zero index then automatically icsCredit will be there in 1st index
        icsCreditValidation(applications)
      } else {
        icsBillValidation(applications)
      }
    } else {
      icsCreditValidation(applications)
    }
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
      // If payer auth is enabled then we will get two transactionIds in relatedTransactions
      // else we will get only one transactionId in relatedTransactions
      // From that, we are not sure which index will have ics_bill / ics_credit information
      // So, we use this icsBillAtIndexZero
      // if it is true, then id at 1st index will have credit information and 0th index will have bill information
      // else id at 0th index will have credit information and 1st index will have bill information
      const icsBillAtZeroIndex = 'ICS_BILL_AT_ZERO_INDEX'

      Cypress.env(icsBillAtZeroIndex, false)

      cy.addDelayBetweenRetries(8000)
      cy.getOrderItems().then(order => {
        callCybersourceAPI(order[paymentTransactionIdEnv]).then(response => {
          expect(response.status).to.equal(200)
          if (payerAuth) {
            /*
            eg: When payer auth enabled, from cybersource API in relatedTransactions array 
            we will get two transactionIds

            tid - 6748193168966998104953
            relatedTransactions -
            "relatedTransactions": [
              {
                "href": "https://apitest.cybersource.com/tss/v2/transactions/6748199174926264604951",
                "method": "GET"
              },
              {
                "href": "https://apitest.cybersource.com/tss/v2/transactions/6748193742576012804953",
                "method": "GET"
              }
            ]            
            */

            expect(response.data._links.relatedTransactions).to.have.lengthOf(2)
            cy.log(
              `Using this transactionId - ${response.data._links.relatedTransactions[0].href
                .split('/')
                .at(-1)}`
            )
            callCybersourceAPI(
              response.data._links.relatedTransactions[0].href.split('/').at(-1)
            ).then(({ status, data }) => {
              expect(status).to.equal(200)
              const { applications } = data.applicationInformation

              // if applications length is 1 then it is ics_bill otherwise ics_credit
              if (applications.length === 1) {
                Cypress.env(icsBillAtZeroIndex, true)
                icsBillValidation(applications)
              } else {
                icsCreditValidation(applications)
              }
            })
          } else {
            expect(response.data._links.relatedTransactions).to.have.lengthOf(1)
          }

          verifyCreditorBill({
            payerAuth,
            transactionId: response.data._links.relatedTransactions[
              payerAuth ? 1 : 0
            ].href
              .split('/')
              .at(-1),
            icsBillAtZeroIndex,
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
  referenceSuffix = false,
  orderIdEnv,
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
            if (referenceSuffix) {
              // In 2.1 testcase we set order_suffix as ORDER_SUFFIX
              // So, verify that here in data.clientReferenceInformation.code
              expect(data.clientReferenceInformation.code).contain(ORDER_SUFFIX)
            } else {
              expect(data.clientReferenceInformation.code).equal(
                item[orderIdEnv].split('-01')[0]
              )
            }

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

            expect(transactionId).to.not.equal(undefined)
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
  { prefix, approved, referenceSuffix },
  { transactionIdEnv, paymentTransactionIdEnv, orderIdEnv }
) {
  if (approved) {
    verifyCyberSourceAPI({
      prefix,
      transactionIdEnv,
      paymentTransactionIdEnv,
      approved,
      referenceSuffix,
      orderIdEnv,
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
