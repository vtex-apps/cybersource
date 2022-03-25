import { FAIL_ON_STATUS_CODE, VTEX_AUTH_HEADER } from './common/constants.js'
import selectors from './common/selectors.js'

// Order Tax API Test Case
Cypress.Commands.add('orderTaxApi', (requestPayload, tax) => {
  cy.getVtexItems().then(vtex => {
    cy.request({
      method: 'POST',
      url: `${vtex.baseUrl}/${
        Cypress.env('workspace').prefix
      }/checkout/order-tax`,
      headers: {
        Authorization: vtex.authorization,
        ...VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken),
      },
      ...FAIL_ON_STATUS_CODE,
      body: requestPayload,
    }).then(response => {
      expect(response.status).to.equal(200)

      let taxFromAPI = 0

      response.body.itemTaxResponse.forEach(item => {
        item.taxes.map(obj => (taxFromAPI += obj.value))
      })
      expect(taxFromAPI.toFixed(2)).to.equal(tax.replace('$ ', ''))
    })
  })
})

// For promotional product 5seconds seems to be very small

// Set Product Quantity
function setProductQuantity({ position, quantity }, subTotal, check) {
  cy.intercept('**/update').as('update')

  cy.get(selectors.ProductQuantityInCheckout(position))
    .should('be.visible')
    .should('not.be.disabled')
    .focus()
    .type(`{backspace}${quantity}{enter}`)
  cy.get(selectors.ItemRemove(position)).should(
    'not.have.css',
    'display',
    'none'
  )
  cy.wait('@update', { timeout: 8000 })

  if (check) {
    cy.get(selectors.SubTotal, { timeout: 5000 }).should('have.text', subTotal)
  }
}

Cypress.Commands.add(
  'updateProductQuantity',
  (
    product,
    { quantity = '1', multiProduct = false, verifySubTotal = true } = {}
  ) => {
    cy.get(selectors.CartTimeline).should('be.visible').click({ force: true })
    cy.get(selectors.ShippingPreview).should('be.visible')
    if (multiProduct) {
      // Set First product quantity and don't verify subtotal because we passed false
      setProductQuantity({ position: 1, quantity }, product.subTotal, false)
      // if multiProduct is true, then remove the set quantity and verify subtotal for multiProduct
      // Set second product quantity and verify subtotal
      setProductQuantity(
        { position: 2, quantity: 1 },
        product.subTotal,
        verifySubTotal
      )
    } else {
      // Set First product quantity and verify subtotal
      setProductQuantity(
        { position: 1, quantity },
        product.subTotal,
        verifySubTotal
      )
    }
  }
)
