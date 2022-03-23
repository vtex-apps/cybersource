import { verifyTotal } from './common/support.js'
import { FAIL_ON_STATUS_CODE } from './common/constants.js'

Cypress.Commands.add('verifyTotal', verifyTotal)

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
