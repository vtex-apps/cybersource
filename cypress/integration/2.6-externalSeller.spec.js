import {
    testSetup,
    updateRetry,
  } from '../support/common/support.js'
  import { externalSeller } from '../support/sandbox_outputvalidation'
  import selectors from '../support/common/selectors.js'
import { getTestVariables } from '../support/utils.js'
import { completePayment, verifyAntiFraud, verifyStatusInInteractionAPI } from '../support/testcase.js'
  
  describe('Discount Shipping Testcase', () => {
    testSetup()

    const { prefix, product1Name, product2Name, tax, totalAmount, postalCode, externalSaleEnv } =
    externalSeller

  const { transactionIdEnv } = getTestVariables(prefix)
  
    it('Adding Product to Cart', updateRetry(3), () => {
        // Search the product
        cy.searchProduct(product1Name)
        // Add product to cart
        cy.addProduct(product1Name, { proceedtoCheckout: false})
        // Search the product
        cy.searchProduct(product2Name)
        // Add product to cart
        cy.addProduct(product2Name, { proceedtoCheckout: true})
    })

    it('Updating product quantity to 1', updateRetry(3), () => {
        // Update Product quantity to 1
        cy.updateProductQuantity(externalSeller, {quantity:'1'})
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

    completePayment(prefix, externalSaleEnv)

    verifyStatusInInteractionAPI(prefix, externalSaleEnv, transactionIdEnv)
  
    verifyAntiFraud(prefix, transactionIdEnv)
  })
  