import { configureTaxConfigurationInOrderForm } from '../support/common/testcase.js'
import { ENTITIES } from '../support/common/constants.js'

describe('Wiping the environment', () => {
  configureTaxConfigurationInOrderForm('')

  it('Getting user & then deleting addresses associated with that user', () => {
    cy.getVtexItems().then(vtex => {
      cy.searchInMasterData(ENTITIES.CLIENTS, vtex.robotMail).then(clients => {
        cy.searchInMasterData(ENTITIES.ADDRESSES, clients[0].id).then(
          addresses => {
            for (const { id } of addresses) {
              cy.deleteDocumentInMasterData(ENTITIES.ADDRESSES, id)
            }
          }
        )
      })
    })
  })
})
