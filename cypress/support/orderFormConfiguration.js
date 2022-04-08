import { FAIL_ON_STATUS_CODE, VTEX_AUTH_HEADER } from './common/constants.js'

const config = Cypress.env()

// Constants
const { account } = config.base.vtex

const ORDER_FORM_CONFIG = `https://${account}.vtexcommercestable.com.br/api/checkout/pvt/configuration/orderForm`

function callOrderFormConfiguration(vtex) {
  cy.request({
    method: 'GET',
    url: ORDER_FORM_CONFIG,
    headers: VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken),
    ...FAIL_ON_STATUS_CODE,
  })
    .as('ORDERFORM')
    .its('status')
    .should('equal', 200)

  return cy.get('@ORDERFORM')
}

export function configureTaxConfigurationInOrderForm(workspace = null) {
  it(`Configuring tax configuration in Order Form Configuration API`, () => {
    cy.getVtexItems().then(vtex => {
      callOrderFormConfiguration(vtex).then(response => {
        response.body.taxConfiguration = workspace
          ? {
              url: `https://${workspace}--${vtex.account}.myvtex.com/cybersource/checkout/order-tax`,
              authorizationHeader: vtex.authorizationHeader,
              allowExecutionAfterErrors: false,
              integratedAuthentication: false,
              appId: null,
            }
          : {}
        cy.request({
          method: 'POST',
          url: ORDER_FORM_CONFIG,
          headers: VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken),
          ...FAIL_ON_STATUS_CODE,
          body: response.body,
        })
          .its('status')
          .should('equal', 204)
      })
    })
  })
}

export function startCyberSource(workspace) {
  it('Start cybersource', () => {
    cy.getVtexItems().then(vtex => {
      callOrderFormConfiguration(vtex).then(response => {
        if (!response.body.taxConfiguration.url.includes(workspace)) {
          expect(response.body.taxConfiguration).to.be.null
        }
      })
    })
  })
}
