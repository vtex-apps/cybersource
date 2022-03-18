import { FAIL_ON_STATUS_CODE } from './common_constants.js'

Cypress.Commands.add('getVtexItems', () => {
  return cy.wrap(Cypress.env().base.vtex, { log: false })
})

// Get API Test Case
Cypress.Commands.add('getAPI', (url, headers) => {
  cy.request({
    method: 'GET',
    url,
    headers,
    ...FAIL_ON_STATUS_CODE,
  })
})
