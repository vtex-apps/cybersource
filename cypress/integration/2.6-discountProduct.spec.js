import { loginViaCookies, updateRetry } from '../support/common/support.js'
import selectors from '../support/common/selectors.js'
import { discountProduct } from '../support/outputvalidation'
import { getTestVariables } from '../support/utils.js'
import { paymentTestCases, orderTaxAPITestCase } from '../support/testcase.js'

describe('Discount Product Testcase', () => {
  loginViaCookies()

  const { prefix, productName, env, tax, totalAmount, postalCode } =
    discountProduct

  // Verify tax via order tax api
  orderTaxAPITestCase(prefix, tax)

  it('Adding Product to Cart', updateRetry(3), () => {
    // Search the product
    cy.searchProduct(productName)
    // Add product to cart
    cy.addProduct(productName, { proceedtoCheckout: true })
  })

  it('Updating product quantity to 1', updateRetry(3), () => {
    cy.checkForTaxErrors()
    // Update Product quantity to 1
    cy.updateProductQuantity(discountProduct, {
      quantity: '1',
    })
  })

  it('Updating Shipping Information', updateRetry(3), () => {
    cy.checkForTaxErrors()
    // Update Shipping Section
    cy.updateShippingInformation({ postalCode, timeout: 8000 })
  })

  it('Verifying tax and total amounts,discount for a discounted product', () => {
    // Verify Tax
    cy.get(selectors.TaxAmtLabel).last().should('have.text', tax)
    // Verify Total
    cy.verifyTotal(totalAmount)
    // Verify Discounts
    cy.get(selectors.Discounts).last().should('be.visible')
  })

  paymentTestCases(
    discountProduct,
    { prefix, approved: false, payerAuth: true },
    { ...getTestVariables(prefix), orderIdEnv: env }
  )
})
