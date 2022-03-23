import { VTEX_AUTH_HEADER, FAIL_ON_STATUS_CODE } from './common/constants.js'
import { updateRetry } from './common/support.js'
import { invoiceAPI, transactionAPI } from './common/apis.js'
import selectors from './common/selectors.js'
import { orderAndSaveProductId } from './utils.js'

export function completePayment(prefix, orderIdEnv) {
  it(`In ${prefix} - Completing the Payment & save OrderId`, () => {
    cy.intercept('**/gatewayCallback/**').as('callback')
    cy.get(selectors.CreditCard).click()
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

      cy.getIframeBody(selectors.PaymentMethodIFrame)
        .find(selectors.CreditCardCode)
        .type('123')
      cy.get(selectors.BuyNowBtn).last().click()
      cy.wait('@callback')
        .its('response.statusCode', { timeout: 5000 })
        .should('eq', 204)
      orderAndSaveProductId(orderIdEnv)
    })
  })
}

export function verifyStatusInInteractionAPI(
  prefix,
  orderIdEnv,
  transactionIdEnv
) {
  it(
    `In ${prefix} - Verifying status & message in transaction/interaction API`,
    updateRetry(3),
    () => {
      const expectedStatus = 'Payment Started'
      const expectedMessage = 'Authorization response parsed'

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
              expect(response.status).to.equal(200)
              const index = interactionResponse.body.findIndex(
                ob =>
                  ob.Status === expectedStatus &&
                  ob.Message.includes(expectedMessage)
              )

              if (index >= 0) {
                const jsonString =
                  interactionResponse.body[index].Message.split(': ')

                if (jsonString) {
                  const json = JSON.parse(jsonString[1])

                  expect(json.status).to.match(/approved|undefined/i)
                  expect(json.message).to.match(/authorized|review/i)
                }
              } else {
                throw new Error(
                  `Unable to find expected Status: ${expectedStatus} and Message: ${expectedMessage} in transaction/interactions response`
                )
              }
            })
          })
        })
      })
    }
  )
}

export function verifyAntiFraud(prefix, transactionIdEnv) {
  it(`In ${prefix} - Verifying AntiFraud status`, () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        cy.getAPI(
          `${vtex.baseUrl}/cybersource-fraud/payment-provider/transactions/${order[transactionIdEnv]}`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(response => {
          expect(response.status).to.equal(200)
          expect(response.body.status).to.match(/approved|undefined|denied/i)
        })
      })
    })
  })
}

export function verifyCyberSourceAPI(prefix, transactionIdEnv, fn = null) {
  it.only(`In ${prefix} - Verifying cybersource API status`, () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        cy.getAPI(
          `${transactionAPI(vtex.baseUrl)}/${order[transactionIdEnv]}/payments`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(response => {
          expect(response.status).to.equal(200)
          cy.log(response.body[0].tid)
          cy.request({
            url: `${vtex.cybersourceapi}/6476351198506781104003`,
            headers: {
              signature: vtex.signature,
              'v-c-merchant-id': vtex.merchantId,
              'v-c-date': new Date().toUTCString(),
            },
            ...FAIL_ON_STATUS_CODE,
          }).then(resp => {
            cy.log(resp)
            expect(resp.status).to.equal(200)
            fn && fn()
          })
        })
      })
    })
  })
}
