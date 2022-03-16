// ***********************************************
// Keeps the variables of the environment
// ***********************************************

// File to persist env between tests
const ordersJson = '.orders.json'

// Set VTEX item
Cypress.Commands.add('setOrderItem', (orderItem, orderValue) => {
  cy.readFile(ordersJson).then(items => {
    items[orderItem] = orderValue
    cy.writeFile(ordersJson, items)
  })
})

// Get VTEX vars
Cypress.Commands.add('getOrderItems', () => {
  cy.readFile(ordersJson).then(items => {
    return items
  })
})
