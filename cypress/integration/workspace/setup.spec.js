import { stopTestCaseOnFailure } from '../../support/cypress-template/common_support.js'

// Login page model
const TXT_EMAIL = '[name = "email"]'
const TXT_PASSWORD = '[name = "password"]'
const TXT_CODE = '[name = "code"]'

describe('Setting up the environment', () => {
  const WORKSPACE = 'master'

  // Test if VTEX CLI is installed doing a logout
  it('Checking VTEX CLI', () => {
    cy.vtex('logout').its('stdout').should('contain', 'See you soon')
  })

  // Set variables to be used in all next tests
  it('Setting global variables', () => {
    cy.setVtexItem('WORKSPACE', WORKSPACE)
    cy.setVtexItem('ACCOUNT', Cypress.env('VTEX_ACCOUNT'))
    cy.setVtexItem(
      'BASE_URL',
      `https://${WORKSPACE}--${Cypress.env('VTEX_ACCOUNT')}.myvtex.com`
    )
    cy.setVtexItem(
      'ID_URL',
      'https://vtexid.vtex.com.br/api/vtexid/pub/authenticate/default'
    )
    cy.setVtexItem('ROBOT_MAIL', Cypress.env('VTEX_ROBOT_MAIL'))
    cy.setVtexItem('ROBOT_PASSWORD', Cypress.env('VTEX_ROBOT_PASSWORD'))
    cy.setVtexItem('API_KEY', Cypress.env('VTEX_API_KEY'))
    cy.setVtexItem('API_TOKEN', Cypress.env('VTEX_API_TOKEN'))
    cy.setVtexItem('COOKIE_NAME', Cypress.env('VTEX_COOKIE_NAME'))
  })

  // Get Cookie Credential
  it('Getting Admin auth cookie value', () => {
    // Get Cookie Credencial
    cy.getVtexItems().then(vtex => {
      cy.request({
        method: 'GET',
        url: vtex.ID_URL,
        qs: { user: vtex.API_KEY, pass: vtex.API_TOKEN },
      }).then(response => {
        expect(response.body).property('authStatus').to.equal('Success')
        cy.setVtexItem('API_COOKIE', response.body.authCookie.Value)
        cy.setVtexItem(
          'AUTH_URL',
          `https://${vtex.ACCOUNT}.myvtex.com/api/vtexid/pub/authentication`
        )
      })
    })
  })

  // Start VTEX CLI Authentication (the VTEX CLI is a version modified to work in that way)
  it('Autenticating on VTEX CLI', () => {
    // Authenticate as Robot
    cy.getVtexItems().then(vtex => {
      // Try to load VTEX CLI callback URL
      cy.exec('cat .vtex.url', { timeout: 10000 }).then(callbackUrl => {
        cy.visit(callbackUrl.stdout)

        // Intercept doesn't work, you must wait two seconds
        cy.wait(2000) // eslint-disable-line cypress/no-unnecessary-waiting

        cy.get('body').then($body => {
          if ($body.find(TXT_EMAIL).length) {
            // Fill Robot email
            cy.get(TXT_EMAIL)
              .should('be.visible')
              .type(`${vtex.ROBOT_MAIL}{enter}`, { log: false })

            // Fill Robot password
            cy.get(TXT_PASSWORD)
              .should('be.visible')
              .type(`${vtex.ROBOT_PASSWORD}{enter}`, { log: false })
          }
        })

        // Sometimes the system ask for SMS Code
        // You must wait do see if it is the case
        cy.wait(2000) // eslint-disable-line cypress/no-unnecessary-waiting

        // Fill Robot SMS code
        cy.get('body').then($body => {
          if ($body.find(TXT_CODE).length) {
            // Get SMS Code
            const OTP = 'cypress/plugins/otp.sh'

            cy.exec(OTP).then(smsCode => {
              cy.get(TXT_CODE)
                .should('be.visible')
                .type(`${smsCode.stdout}{enter}`)
            })
          }
        })
      })

      // Wait for the authentication process to finish
      cy.get('body').should('contain', 'You may now close this window.')
    })
  })

  // Get Robot Cookie and saving it
  it('Getting Robot cookie value', () => {
    cy.vtex('whoami').its('stdout').should('contain', 'Logged into')
    cy.vtex('local token').then(cookie => {
      cy.setVtexItem('ROBOT_COOKIE', cookie.stdout)
    })
  })

  // Link APP to test, Cybersource in this case
  it('Linking App to be tested', { retries: 2 }, () => {
    cy.vtex('link')
  })

  it('Checking if Cybersource was linked', { retries: 2 }, () => {
    cy.vtex('ls | grep Linked -A 3')
      .its('stdout')
      .should('contains', 'cybersource')
  })
})

// Fail all if any test fails
stopTestCaseOnFailure()
