import { VTEX_AUTH_HEADER } from './common/constants.js'

export function verifyAntiFraud(type, transactionIdEnv) {
  it(`In ${type} - Verifying AntiFraud status`, () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        cy.getAPI(
          `${vtex.baseUrl}/cybersource-fraud/payment-provider/transactions/${order[transactionIdEnv]}`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(response => {
          expect(response.status).to.equal(200)
          expect(response.body.status).to.match(/approved|undefined|denied/i)
        })
      })
    })
  })
}
