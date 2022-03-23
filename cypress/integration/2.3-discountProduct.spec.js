import { testSetup, updateRetry } from '../support/common/support.js'
import selectors from '../support/common/selectors.js'
import { discountProduct } from '../support/sandbox_outputvalidation.js'
import { getTestVariables } from '../support/utils.js'
import {
  completePayment,
  verifyStatusInInteractionAPI,
  verifyAntiFraud,
} from '../support/testcase.js'

describe('Discount Product Testcase', () => {
  testSetup()

  const { prefix, productName, tax, env, totalAmount, postalCode } =
    discountProduct

  const { transactionIdEnv } = getTestVariables(prefix)

  it('Adding Product to Cart', updateRetry(3), () => {
    // Search the product
    cy.searchProduct(productName)
    // Add product to cart
    cy.addProduct(productName, { proceedtoCheckout: true })
  })

  it('Updating product quantity to 1', updateRetry(3), () => {
    // Update Product quantity to 1
    cy.updateProductQuantity(discountProduct, {
      quantity: '1',
    })
  })

  it('Updating Shipping Information', updateRetry(3), () => {
    // Update Shipping Section
    cy.updateShippingInformation({ postalCode })
  })

  it('Verifying tax and total amounts,discount for a discounted product', () => {
    // Verify Tax
    cy.get(selectors.TaxAmtLabel).last().should('have.text', tax)
    // Verify Total
    cy.verifyTotal(totalAmount)
    // Verify Discounts
    cy.get(selectors.Discounts).last().should('be.visible')
  })

  completePayment(prefix, env)

  verifyStatusInInteractionAPI(prefix, env, transactionIdEnv)

  verifyAntiFraud(prefix, transactionIdEnv)
})
