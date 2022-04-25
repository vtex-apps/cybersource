import { testSetup, updateRetry } from '../support/common/support.js'
import { externalSeller } from '../support/outputvalidation.js'
import selectors from '../support/common/selectors.js'
import { getTestVariables } from '../support/utils.js'
import {
  completePayment,
  verifyCyberSourceAPI,
  verifyStatusInInteractionAPI,
} from '../support/testcase.js'

describe('External Seller Testcase', () => {
  testSetup()

  const {
    prefix,
    product1Name,
    product2Name,
    tax,
    totalAmount,
    postalCode,
    directSaleEnv,
    externalSaleEnv,
    directSaleTax,
    externalSellerTax,
  } = externalSeller

  const { transactionIdEnv, paymentTransactionIdEnv } = getTestVariables(prefix)

  it.skip('Verifying tax amount for external seller product via order-tax API', () => {
    // We have stored externalSeller.json in cypress/fixtures
    // That we are loading using cy.fixture() command and passing as a payload to orderTaxAPI
    cy.fixture('externalSeller').then(requestPayload =>
      cy.orderTaxApi(requestPayload, externalSellerTax)
    )
  })

  it('Verifying tax amount for direct sale product via order-tax API', () => {
    // We have stored singleProduct.json in cypress/fixtures
    // That we are loading using cy.fixture() command and passing to orderTaxAPI
    cy.fixture('directSale').then(requestPayload =>
      cy.orderTaxApi(requestPayload, directSaleTax)
    )
  })

  it('Adding Product to Cart', updateRetry(3), () => {
    // Search the product
    cy.searchProduct(product1Name)
    // Add product to cart
    cy.addProduct(product1Name, { proceedtoCheckout: false })
    // Search the product
    cy.searchProduct(product2Name)
    // Add product to cart
    cy.addProduct(product2Name, { proceedtoCheckout: true })
  })

  it('Updating product quantity to 1', updateRetry(3), () => {
    cy.checkForTaxErrors()
    // Update Product quantity to 1
    cy.updateProductQuantity(externalSeller, { quantity: '1' })
  })

  it('Updating Shipping Information', updateRetry(3), () => {
    cy.checkForTaxErrors()
    // Update Shipping Section
    cy.updateShippingInformation({ postalCode })
  })

  it('Verify tax and total', updateRetry(3), () => {
    // Verify Tax
    cy.get(selectors.TaxAmtLabel).last().should('have.text', tax)
    // Verify Total
    cy.verifyTotal(totalAmount)
  })

  completePayment(prefix, false, externalSeller)

  describe('Testing API for External Sale', () => {
    it('Get External Sale orderId and update in Cypress env', () => {
      cy.getOrderItems().then(order => {
        if (!order[externalSaleEnv]) {
          throw new Error('External Sale Order id is missing')
        }
      })
    })

    verifyStatusInInteractionAPI({
      prefix,
      transactionIdEnv,
      orderIdEnv: externalSaleEnv,
      paymentTransactionIdEnv,
      approved: true,
    })

    describe('Testing API for Direct Sale', () => {
      it('Get Direct Sale orderId and update in Cypress env', () => {
        cy.getOrderItems().then(order => {
          if (!order[directSaleEnv]) {
            throw new Error('Direct Sale Order id is missing')
          }
        })
      })

      verifyStatusInInteractionAPI({
        prefix,
        transactionIdEnv,
        orderIdEnv: externalSaleEnv,
        paymentTransactionIdEnv,
        approved: true,
      })

      verifyCyberSourceAPI({
        prefix,
        transactionIdEnv,
        paymentTransactionIdEnv,
        approved: true,
      })
    })
  })
})
