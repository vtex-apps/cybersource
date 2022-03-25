import { testSetup, updateRetry } from '../support/common/support.js'
import selectors from '../support/common/selectors.js'
import {
  singleProduct,
  requestRefund,
} from '../support/sandbox_outputvalidation.js'
import { getTestVariables } from '../support/utils.js'
import {
  completePayment,
  verifyStatusInInteractionAPI,
  verifyAntiFraud,
  verifyCyberSourceAPI,
  sendInvoiceTestCase,
} from '../support/testcase.js'

describe('Single Product Testcase', () => {
  testSetup()

  const { prefix, productName, tax, totalAmount, postalCode } = singleProduct

  const { paymentTransactionIdEnv, transactionIdEnv } = getTestVariables(prefix)
  const orderIdEnv = requestRefund.fullRefundEnv

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

  completePayment(prefix, orderIdEnv)

  verifyStatusInInteractionAPI(prefix, orderIdEnv, transactionIdEnv)

  sendInvoiceTestCase(totalAmount, orderIdEnv)

  verifyCyberSourceAPI({ prefix, transactionIdEnv, paymentTransactionIdEnv })

  verifyAntiFraud({ prefix, transactionIdEnv, paymentTransactionIdEnv })
})
