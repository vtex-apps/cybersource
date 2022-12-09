import { FAIL_ON_STATUS_CODE, VTEX_AUTH_HEADER } from './common/constants.js'

const CYBERSOURCE_AFFILIATION_ID = '21d78653-50d6-4b06-b553-2645e67a6f5e'

export function setWorkspaceInAffiliation(workspace = null, payerAuth = true) {
  it(`Configuring workspace as '${workspace}' in payment affiliation`, () => {
    cy.getVtexItems().then(vtex => {
      cy.request({
        method: 'GET',
        url: `${vtex.baseUrl}/api/payments/pvt/affiliations/${CYBERSOURCE_AFFILIATION_ID}`,
        headers: VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken),
        ...FAIL_ON_STATUS_CODE,
      }).then(response => {
        expect(response.status).to.equal(200)
        const workspaceIndex = response.body.configuration.findIndex(
          el => el.name === 'workspace'
        )

        const payerAuthIndex = response.body.configuration.findIndex(
          el => el.name === 'Payer Authentication'
        )

        response.body.configuration[workspaceIndex].value = workspace
        response.body.configuration[payerAuthIndex].value = payerAuth
          ? 'active'
          : 'disabled'

        cy.request({
          method: 'PUT',
          url: `${vtex.baseUrl}/api/payments/pvt/affiliations/${CYBERSOURCE_AFFILIATION_ID}`,
          headers: VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken),
          ...FAIL_ON_STATUS_CODE,
          body: response.body,
        })
          .its('status')
          .should('equal', 201)
      })
    })
  })
}
