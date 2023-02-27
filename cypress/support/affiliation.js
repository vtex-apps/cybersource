import { FAIL_ON_STATUS_CODE, VTEX_AUTH_HEADER } from './common/constants.js'

const CYBERSOURCE_AFFILIATION_ID = '21d78653-50d6-4b06-b553-2645e67a6f5e'

export function setWorkspaceInAffiliation(workspace = null, payerAuth = true) {
  it(`Configuring workspace as '${workspace}' in payment affiliation`, () => {
    cy.getVtexItems().then(vtex => {
      cy.qe(`
      curl --location --request GET '${vtex.baseUrl}/api/payments/pvt/affiliations/21d78653-50d6-4b06-b553-2645e67a6f5e' \
--header 'X-VTEX-API-AppKey: AppKey' \
--header 'X-VTEX-API-AppToken: AppToken' \
      `)
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

        const paymentSettleIndex = response.body.configuration.findIndex(
          el => el.name === 'autoSettle'
        )

        const payerAuthIndex = response.body.configuration.findIndex(
          el => el.name === 'Payer Authentication'
        )

        const captureSettingIndex = response.body.configuration.findIndex(
          el => el.name === 'Capture Setting'
        )

        response.body.configuration[workspaceIndex].value = workspace

        response.body.configuration[paymentSettleIndex].value = payerAuth
          ? 'provider'
          : 'after_authorization'
        response.body.configuration[captureSettingIndex].value = payerAuth
          ? 'DelayedCapture'
          : 'AuthAndCapture'
        response.body.configuration[payerAuthIndex].value = payerAuth
          ? 'active'
          : 'disabled'
        cy.qe(`
        curl --location --request PUT '${vtex.baseUrl}/api/payments/pvt/affiliations/21d78653-50d6-4b06-b553-2645e67a6f5e' \
      --header 'X-VTEX-API-AppKey: AppKey' \
      --header 'X-VTEX-API-AppToken: AppToken' \
      --data-raw 'data'
        `)
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
