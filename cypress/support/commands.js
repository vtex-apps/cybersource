import {
  addProduct,
  fillAddress,
  searchProduct,
  updateShippingInformation,
  getIframeBody,
  updateProductQuantity,
  verifyFreeProduct,
  verifyTaxAndTotal,
  closeCart,
} from './cypress-template/common_support.js'
import {
  VTEX_AUTH_HEADER,
  FAIL_ON_STATUS_CODE,
} from './cypress-template/common_constants.js'

Cypress.Commands.add('searchProduct', searchProduct)
Cypress.Commands.add('addProduct', addProduct)
Cypress.Commands.add('updateShippingInformation', updateShippingInformation)
Cypress.Commands.add('getIframeBody', getIframeBody)
Cypress.Commands.add('fillAddress', fillAddress)
Cypress.Commands.add('updateProductQuantity', updateProductQuantity)
Cypress.Commands.add('verifyFreeProduct', verifyFreeProduct)
Cypress.Commands.add('verifyTaxAndTotal', verifyTaxAndTotal)
Cypress.Commands.add('closeCart', closeCart)
// Order Tax API Test Case
Cypress.Commands.add('orderTaxApi', (requestPayload, tax) => {
  cy.getVtexItems().then(vtex => {
    cy.request({
      method: 'POST',
      url: `${vtex.baseUrl}/cybersource/checkout/order-tax`,
      headers: {
        Authorization: vtex.authorization,
        ...VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken),
      },
      ...FAIL_ON_STATUS_CODE,
      body: requestPayload,
    }).then(response => {
      expect(response.status).to.equal(200)

      const reqIds = requestPayload.items.map(({ id }) => id)
      const respIds = response.body.itemTaxResponse.map(({ id }) => id)

      expect(reqIds.sort()).to.deep.equal(respIds.sort())
      let taxFromAPI = 0

      response.body.itemTaxResponse.forEach(item => {
        item.taxes.map(obj => (taxFromAPI += obj.value))
      })
      expect(taxFromAPI.toFixed(2)).to.equal(tax.replace('$ ', ''))
    })
  })
})

Cypress.Commands.add('cybersourceapi', (tid, fn = null) => {
  cy.getVtexItems().then(vtex => {
    cy.request({
      url: `${vtex.cybersourceapi}/${tid}`,
      headers: {
        signature: vtex.signature,
        'v-c-merchant-id': vtex.merchantId,
        'v-c-date': new Date().toUTCString(),
      },
      ...FAIL_ON_STATUS_CODE,
    }).then(resp => {
      expect(resp.status).to.equal(200)
      fn && fn()
    })
  })
})
