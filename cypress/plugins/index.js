// <reference types="cypress" />

// ***********************************************************
// This example plugins/index.js can be used to load plugins
//
// You can change the location of this file or turn off loading
// the plugins file with the 'pluginsFile' configuration option.
//
// You can read more here:
// https://on.cypress.io/plugins-guide
// ***********************************************************

// This function is called when a project is opened or re-opened (e.g. due to
// the project's config changing)

/**
 * @type {Cypress.PluginConfig}
 */
// eslint-disable-next-line no-unused-vars

const moment = require('moment')

const { retrieveTransactions } = require('../support/cybersourceApi.js')

function getXML() {
  const date = moment.utc().format('YYYY-MM-DD HH:mm:ss')

  return `content=<?xml version="1.0" encoding="UTF-8"?>
   <!DOCTYPE CaseManagementOrderStatus SYSTEM "https://ebctest.cybersource.com/ebctest/reports/dtd/cmorderstatus_1_1.dtd">
   <CaseManagementOrderStatus xmlns="http://reports.cybersource.com/reports/cmos/1.0" MerchantID="vtex_dev" Name="Case Management Order Status" Date="${date} GMT" Version="1.1">
     <Update MerchantReferenceNumber="1221062008978" RequestID="6485752156986590004006">
       <OriginalDecision>REVIEW</OriginalDecision>
       <NewDecision>ACCEPT</NewDecision>
       <Reviewer>brian</Reviewer>
       <Notes>
         <Note Date="${date}" AddedBy="brian" Comment="Took ownership." />
       </Notes>
       <Queue>Example</Queue>
       <Profile>Testing</Profile>
     </Update>
   </CaseManagementOrderStatus>`
}

module.exports = (on, _) => {
  // `on` is used to hook into various events Cypress emits
  // `config` is the resolved Cypress config
  on('task', {
    cybersourceAPI: ({ vtex, tid }) => {
      return new Promise((resolve, reject) => {
        retrieveTransactions(vtex, tid, (error, data, response) => {
          if (error) {
            reject(error)
          }

          resolve({ status: response.status, data })
        })
      })
    },
    getXML: () => {
      return getXML()
    },
  })
}
