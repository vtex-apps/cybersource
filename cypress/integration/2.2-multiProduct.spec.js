import { testSetup, updateRetry } from '../support/common/support.js'
import {
  multiProduct,
  requestRefund,
} from '../support/sandbox_outputvalidation.js'
import selectors from '../support/common/selectors.js'
import {
  completePayment,
  verifyStatusInInteractionAPI,
  verifyAntiFraud,
} from '../support/testcase.js'
import { getTestVariables } from '../support/utils.js'

describe('Multi Product Testcase', () => {
  testSetup()

  const { prefix, product1Name, product2Name, tax, totalAmount, postalCode } =
    multiProduct

  const { transactionIdEnv } = getTestVariables(prefix)
  const orderIdEnv = requestRefund.partialRefundEnv

  it('Adding Product to Cart', updateRetry(3), () => {
    // Search the product
    cy.searchProduct(product1Name)
    // Add product to cart
    cy.addProduct(product1Name, { proceedtoCheckout: false })
    // Search the product
    cy.searchProduct(product2Name)
    // Add product to cart
    cy.addProduct(product2Name, {
      proceedtoCheckout: true,
    })
  })

  it('Updating product quantity to 2', updateRetry(3), () => {
    // Update Product quantity to 2
    cy.updateProductQuantity(multiProduct, { quantity: '2' })
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

  completePayment(prefix, orderIdEnv)

  verifyStatusInInteractionAPI(prefix, orderIdEnv, transactionIdEnv)

  verifyAntiFraud(prefix, transactionIdEnv)
})
