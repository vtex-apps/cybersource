import {
    testSetup,
    updateRetry,
    orderAndSaveProductId,
  } from '../support/cypress-template/common_support.js'
  import { promotionProduct } from '../support/sandbox.outputValidation'
  import { PRODUCTS } from '../support/cypress-template/sandbox_products.js'
  import selectors from '../support/cypress-template/common_selectors.js'
  import {
    invoiceAPI,
    transactionAPI,
  } from '../support/cypress-template/common_apis.js'
  import { VTEX_AUTH_HEADER } from '../support/cypress-template/common_constants.js'
  
  describe('Promotional Product Testcase', () => {
    testSetup()
  
    const orderEnv = 'promotion1'
    const product = PRODUCTS.invicta
    const expectedStatus = 'Payment Started'
    const expectedMessage = 'Authorization response parsed'
  
    it('Adding Product to Cart', updateRetry(3), () => {
      // Search the product
      cy.searchProduct(product)
      // Add product to cart
      cy.addProduct(product, { proceedtoCheckout: true })
    })

    it('Updating product quantity to 2', updateRetry(3), () => {
        // Update Product quantity to 2
        cy.updateProductQuantity(promotionProduct, '2')
    })

    it('Verify tax and total', updateRetry(3), () => {
        // Verify Product tax and total
        cy.verifyTaxAndTotal(promotionProduct)
    })

    it('Verify free product is added', updateRetry(3), () => {
        // Verify free product is added
        cy.verifyFreeProduct()
    })
  
    it('Updating Shipping Information', updateRetry(3), () => {
      // Update Shipping Section
      cy.updateShippingInformation('33180')
    })
  
    it('Completing the Payment & save OrderId', () => {
      cy.intercept('**/gatewayCallback/**').as('callback')
      cy.get(selectors.CreditCard).click()
      cy.getIframeBody(selectors.PaymentMethodIFrame)
        .find(selectors.CreditCardCode)
        .should('be.visible')
      cy.getIframeBody(selectors.PaymentMethodIFrame).then($body => {
        if (!$body.find(selectors.CardExist).length) {
          // Credit cart not exist
          cy.getIframeBody(selectors.PaymentMethodIFrame)
            .find(selectors.CreditCardNumber)
            .type('5555 5555 5555 4444')
          cy.getIframeBody(selectors.PaymentMethodIFrame)
            .find(selectors.CreditCardHolderName)
            .type('Syed')
          cy.getIframeBody(selectors.PaymentMethodIFrame)
            .find(selectors.CreditCardExpirationMonth)
            .select('01')
          cy.getIframeBody(selectors.PaymentMethodIFrame)
            .find(selectors.CreditCardExpirationYear)
            .select('30')
        }
  
        cy.getIframeBody(selectors.PaymentMethodIFrame)
          .find(selectors.CreditCardCode)
          .type('123')
        cy.get(selectors.BuyNowBtn).last().click()
        cy.wait('@callback')
          .its('response.statusCode', { timeout: 5000 })
          .should('eq', 204)
        orderAndSaveProductId(orderEnv)
      })
    })
  
    it(
      'Verifying status & message in transaction/interaction API',
      updateRetry(3),
      () => {
        cy.getVtexItems().then(vtex => {
          cy.getOrderItems().then(item => {
            cy.getAPI(
              `${invoiceAPI(vtex.baseUrl)}/${item[orderEnv]}`,
              VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
            ).then(response => {
              expect(response.status).to.equal(200)
              const [{ transactionId }] = response.body.paymentData.transactions
  
              cy.getAPI(
                `${transactionAPI(vtex.baseUrl, transactionId)}/interactions`,
                VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
              ).then(interactionResponse => {
                const index = interactionResponse.body.findIndex(
                  ob =>
                    ob.Status === expectedStatus &&
                    ob.Message.includes(expectedMessage)
                )
  
                if (index >= 0) {
                  const jsonString =
                    interactionResponse.body[index].Message.split(': ')
  
                  if (jsonString) {
                    const json = JSON.parse(jsonString[1])
  
                    expect(json.status).to.match(/approved|undefined/i)
                    expect(json.message).to.match(/authorized|review/i)
                  }
                } else {
                  throw new Error(
                    `Unable to find expected Status: ${expectedStatus} and Message: ${expectedMessage} in transaction/interactions response`
                  )
                }
              })
            })
          })
        })
      }
    )
  })
  