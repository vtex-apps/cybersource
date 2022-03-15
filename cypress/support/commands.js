import {
  addProduct,
  searchProduct,
  updateShippingInformation,
  getIframeBody,
} from './cypress-template/common_support.js'

Cypress.Commands.add('searchProduct', searchProduct)
Cypress.Commands.add('addProduct', addProduct)
Cypress.Commands.add('updateShippingInformation', updateShippingInformation)
Cypress.Commands.add('getIframeBody', getIframeBody)

// Get VTEX vars
Cypress.Commands.add('getVtexItems', () => {
  return cy.wrap(Cypress.env().base.vtex, { log: false })
})
