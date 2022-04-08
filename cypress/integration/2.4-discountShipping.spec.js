import { testSetup, updateRetry } from '../support/common/support.js'
import { discountShipping } from '../support/outputvalidation.js'
import selectors from '../support/common/selectors.js'
import { getTestVariables } from '../support/utils.js'
import { paymentAndAPITestCases } from '../support/testcase.js'

describe('Discount Shipping Testcase', () => {
  testSetup()

  const { prefix, productName, tax, totalAmount, postalCode, env } =
    discountShipping

  it('Adding Product to Cart', updateRetry(3), () => {
    // Search the product
    cy.searchProduct(productName)
    // Add product to cart
    cy.addProduct(productName, { proceedtoCheckout: true })
  })

  it('Updating product quantity to 1', updateRetry(3), () => {
    // Update Product quantity to 1
    cy.updateProductQuantity(discountShipping, { quantity: '1' })
  })

  it('Updating Shipping Information', updateRetry(3), () => {
    // Update Shipping Section
    cy.updateShippingInformation({ postalCode })
  })

  it('Verify tax and total', updateRetry(3), () => {
    // Verify Tax
    cy.get(selectors.TaxAmtLabel).last().should('have.text', tax)
    // Verify Total
    cy.verifyTotal(totalAmount)
  })

  paymentAndAPITestCases(
    discountShipping,
    { prefix, approved: true },
    { ...getTestVariables(prefix), orderIdEnv: env }
  )
})
