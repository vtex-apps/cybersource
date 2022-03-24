import {
    testSetup,
    updateRetry,
  } from '../support/common/support.js'
  import { discountShipping } from '../support/sandbox_outputvalidation'
  import selectors from '../support/common/selectors.js'
import { getTestVariables } from '../support/utils.js'
import { completePayment, verifyAntiFraud, verifyStatusInInteractionAPI } from '../support/testcase.js'
  
  describe('Discount Shipping Testcase', () => {
    testSetup()

    const { prefix, productName, tax, totalAmount, postalCode,env } =
    discountShipping

  const { transactionIdEnv } = getTestVariables(prefix)
  
    it('Adding Product to Cart', updateRetry(3), () => {
        // Search the product
        cy.searchProduct(productName)
        // Add product to cart
        cy.addProduct(productName, { proceedtoCheckout: true})
    })

    it('Updating product quantity to 2', updateRetry(3), () => {
        // Update Product quantity to 2
        cy.updateProductQuantity(discountShipping, {quantity:'2'})
    })
    
    it('Verify tax and total', updateRetry(3), () => {
      // Verify Tax
      cy.get(selectors.TaxAmtLabel).last().should('have.text', tax)
      // Verify Total
      cy.verifyTotal(totalAmount)
    })
  
    it('Updating Shipping Information', updateRetry(3), () => {
      // Update Shipping Section
      cy.updateShippingInformation({ postalCode })
    })

    completePayment(prefix, env)

    verifyStatusInInteractionAPI(prefix, env, transactionIdEnv)
  
    verifyAntiFraud(prefix, transactionIdEnv)
  })
  