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

// Files to save state
// MUST exist on the plase EMPTY
const vtexJson = '.vtex.json'

// Save VTEX vars
Cypress.Commands.add('setVtexItem', (vtexItem, vtexValue) => {
  cy.readFile(vtexJson).then(items => {
    items[vtexItem] = vtexValue
    cy.writeFile(vtexJson, items)
  })
})

// Get VTEX vars
Cypress.Commands.add('getVtexItems', () => {
  cy.readFile(vtexJson).then(items => {
    return items
  })
})

// Run VTEX CLI commands
Cypress.Commands.add('vtex', command => {
  const VTEX_BIN = Cypress.env('VTEX_BIN')
  const LONG_TIME_OUT = 120000
  const SHORT_TIME_OUT = 20000

  switch (command.split(' ')[0]) {
    case 'workspace':
      return cy.exec(`echo y | ${VTEX_BIN} ${command}`, {
        timeout: SHORT_TIME_OUT,
      })

    case 'link':
      return cy.exec(`echo y | ${VTEX_BIN} ${command} --no-watch`, {
        timeout: LONG_TIME_OUT,
      })

    default:
      return cy.exec(`${VTEX_BIN} ${command}`, {
        timeout: SHORT_TIME_OUT,
      })
  }
})
