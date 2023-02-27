import { loginViaCookies, updateRetry } from '../support/common/support.js'
import selectors from '../support/common/selectors.js'
import { singleProduct, requestRefund } from '../support/outputvalidation.js'
import { getTestVariables } from '../support/utils.js'
import { paymentTestCases, orderTaxAPITestCase } from '../support/testcase.js'

describe('Single Product Testcase', () => {
  loginViaCookies()

  const { prefix, productName, tax, totalAmount, postalCode } = singleProduct

  const orderIdEnv = requestRefund.fullRefundEnv

  // Verify tax via order tax api
  orderTaxAPITestCase(prefix, tax)

  it('Adding Product to Cart', updateRetry(3), () => {
    // Search the product
    cy.searchProduct(productName)
    // Add product to cart
    cy.addProduct(productName, { proceedtoCheckout: true })
  })

  it('Updating product quantity to 2', updateRetry(3), () => {
    cy.checkForTaxErrors()
    // Update Product quantity to 2
    cy.updateProductQuantity(singleProduct, {
      quantity: '2',
    })
  })

  it('Updating Shipping Information', updateRetry(4), () => {
    cy.checkForTaxErrors()
    // Update Shipping Section
    cy.updateShippingInformation({ postalCode })
  })

  it('Verifying tax and total amounts for a product with quantity 2', () => {
    // Verify Tax
    cy.qe(`Verifying the tax should have ${tax}`)
    cy.get(selectors.TaxAmtLabel).last().should('have.text', tax)
    // Verify Total
    cy.verifyTotal(totalAmount)
  })

  paymentTestCases(
    singleProduct,
    { prefix, approved: false, payerAuth: true },
    { ...getTestVariables(prefix), orderIdEnv }
  )
})
