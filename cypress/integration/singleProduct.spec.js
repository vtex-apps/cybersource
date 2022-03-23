import { testSetup, updateRetry } from '../support/common/support.js'
import selectors from '../support/common/selectors.js'
import { invoiceAPI, transactionAPI } from '../support/common/apis.js'
import { VTEX_AUTH_HEADER } from '../support/common/constants.js'
import {
  singleProduct,
  requestRefund,
} from '../support/sandbox_outputvalidation.js'
import { getTestVariables, orderAndSaveProductId } from '../support/utils.js'
import { verifyAntiFraud } from '../support/testcase.js'

describe('Single Product Testcase', () => {
  testSetup()

  const { prefix, productName, tax, totalAmount, postalCode } = singleProduct

  const { transactionIdEnv } = getTestVariables(prefix)

  const expectedStatus = 'Payment Started'
  const expectedMessage = 'Authorization response parsed'

  it('Adding Product to Cart', updateRetry(3), () => {
    // Search the product
    cy.searchProduct(productName)
    // Add product to cart
    cy.addProduct(productName, { proceedtoCheckout: true })
  })

  it('Updating product quantity to 2', updateRetry(3), () => {
    // Update Product quantity to 2
    cy.updateProductQuantity(singleProduct, {
      quantity: '2',
    })
  })

  it('Updating Shipping Information', updateRetry(3), () => {
    // Update Shipping Section
    cy.updateShippingInformation({ postalCode })
  })

  it('Verifying tax and total amounts for a product with quantity 2', () => {
    // Verify Tax
    cy.get(selectors.TaxAmtLabel).last().should('have.text', tax)
    // Verify Total
    cy.verifyTotal(totalAmount)
  })

  it('Completing the Payment & save OrderId', () => {
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
      orderAndSaveProductId(requestRefund.fullRefundEnv)
    })
  })

  it(
    'Verifying status & message in transaction/interaction API',
    updateRetry(3),
    () => {
      cy.getVtexItems().then(vtex => {
        cy.getOrderItems().then(item => {
          cy.getAPI(
            `${invoiceAPI(vtex.baseUrl)}/${item[requestRefund.fullRefundEnv]}`,
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

  verifyAntiFraud(prefix, transactionIdEnv)
})
