import {
  testSetup,
  updateRetry,
} from '../support/cypress-template/common_support.js'
import { PRODUCTS } from '../support/cypress-template/sandbox_products.js'
import selectors from '../support/cypress-template/common_selectors.js'

describe('Payment Testcase', () => {
  testSetup()
  const product = PRODUCTS.shoesv5

  it('Adding Product to Cart', updateRetry(3), () => {
    // Search the product
    cy.searchProduct(product)
    // Add product to cart
    cy.addProduct(product, true)
  })

  it('Updating Shipping Information', updateRetry(3), () => {
    // Update Shipping Section
    cy.updateShippingInformation('33180', true)
  })

  it('Complete the Payment', () => {
    cy.intercept('**/gatewayCallback/**').as('callback')
    cy.get(selectors.CreditCard).click()
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
        .its('response.statusCode', { timeout: 10000 })
        .should('eq', 204)
    })
  })
})
