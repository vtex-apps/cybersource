import { testSetup, updateRetry } from '../support/common/support.js'
import selectors from '../support/common/selectors.js'
import { promotionProduct } from '../support/outputvalidation.js'
import { getTestVariables } from '../support/utils.js'
import {
  paymentAndAPITestCases,
  verifyPaymentSettled,
} from '../support/testcase.js'

describe('Promotional Product Testcase', () => {
  testSetup()

  const { prefix, productName, tax, totalAmount, env, postalCode } =
    promotionProduct

  it('Adding Product to Cart', updateRetry(3), () => {
    // Search the product
    cy.searchProduct(productName)
    // Add product to cart
    cy.addProduct(productName, { proceedtoCheckout: true })
  })

  it('Updating product quantity to 2', updateRetry(3), () => {
    // Update Product quantity to 2
    cy.updateProductQuantity(promotionProduct, {
      quantity: '2',
      timeout: 20000,
    })
  })

  it('Updating Shipping Information', updateRetry(3), () => {
    // Update Shipping Section
    cy.updateShippingInformation({ postalCode })
  })

  it('Verifying tax, total and discounts amounts for a product with quantity 2', () => {
    // Verify Tax
    cy.get(selectors.TaxAmtLabel).last().should('have.text', tax)
    // Verify Total
    cy.verifyTotal(totalAmount)
    // Verify Discounts
    cy.get(selectors.Discounts).last().should('be.visible')
  })

  it('Verify free product is added', updateRetry(3), () => {
    // Verify free product is added
    cy.get('span[class="new-product-price"]')
      .first()
      .should('have.text', 'Free')
  })

  paymentAndAPITestCases(
    promotionProduct,
    { prefix, approved: false },
    { ...getTestVariables(prefix), orderIdEnv: env }
  )

  verifyPaymentSettled(promotionProduct.prefix, promotionProduct.env)
})
