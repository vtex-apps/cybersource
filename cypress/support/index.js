import './common/commands'
import './common/api_commands'
import './common/env_orders'
import './commands.js'

Cypress.Cookies.defaults({
  preserve: /VtexIdclientAutCookie/,
})

Cypress.on('uncaught:exception', (_, __) => {
  return false
})
