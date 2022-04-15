import { FAIL_ON_STATUS_CODE, VTEX_AUTH_HEADER } from './common/constants.js'

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

Cypress.Commands.add('checkForTaxErrors', () => {
  cy.contains('communication error with Tax System has occurred', {
    timeout: 2000,
  }).should('not.exist')
  cy.get('p[class*=vtex-front-messages]', { timeout: 2000 }).should('not.exist')
})
