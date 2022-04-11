import { VTEX_AUTH_HEADER } from './common/constants.js'
import { transactionAPI } from './common/apis.js'
import { transactionConstants } from './transaction_constants.js'
import { updateRetry } from './common/support.js'
import { addDelayBetweenRetries } from './common/utils.js'

function generatetidEnv(prefix) {
  return `${prefix}-tid`
}

function checkTransactionIdIsAvailable(transactionIdEnv) {
  if (!transactionIdEnv) {
    throw new Error('Transaction Id is undefined')
  }
}

export function verifyTransactionAPITestCase({ transactionIdEnv }, status) {
  it('Verify Transaction', updateRetry(3), () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        checkTransactionIdIsAvailable(order[transactionIdEnv])
        addDelayBetweenRetries(3000)
        cy.getAPI(
          `${transactionAPI(vtex.baseUrl)}/${order[transactionIdEnv]}`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(response => {
          expect(response.status).to.equal(200)
          expect(response.body.status).to.equal(status)
          expect(response.body.id).to.equal(order[transactionIdEnv])
          cy.log('Transaction', response)
        })
      })
    })
  })
}

export function verifyTransactionSettlementsAPITestCase(
  { transactionIdEnv, productTotalEnv },
  autoSettlement = true
) {
  it('Verify Transaction Settlement', updateRetry(2), () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        checkTransactionIdIsAvailable(order[transactionIdEnv])
        cy.getAPI(
          `${transactionAPI(vtex.baseUrl)}/${
            order[transactionIdEnv]
          }/settlements`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(response => {
          expect(response.status).to.equal(200)
          // TODO: Request doesnt give us array it is empty. Removing for now
          // expect(response.body.requests.length).to.be.greaterThan(0)
          expect(response.body.actions.length).to.be.greaterThan(0)

          if (autoSettlement) {
            expect(response.body.actions[0].type).to.equal(
              transactionConstants.AutoSettlement
            )
          } else {
            expect(response.body.actions[0].type).to.equal(
              transactionConstants.UpOnRquest
            )
          }

          expect(response.body.actions[0].value).to.equal(
            order[productTotalEnv]
          )
        })
      })
    })
  })
}

export function verifyTransactionPaymentsAPITestCase(
  { transactionIdEnv, productTotalEnv },
  fn
) {
  it('Verify Transaction Payment', updateRetry(2), () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        checkTransactionIdIsAvailable(order[transactionIdEnv])
        cy.getAPI(
          `${transactionAPI(vtex.baseUrl)}/${order[transactionIdEnv]}/payments`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(response => {
          expect(response.status).to.equal(200)
          // Store tid in .orders.json
          cy.setOrderItem(
            generatetidEnv(transactionIdEnv),
            response.body[0].tid
          )
          expect(response.body[0].paymentSystemName).to.equal(
            transactionConstants.PayPalCP
          )
          expect(response.body[0].paymentSystemName).to.equal(
            transactionConstants.PayPalCP
          )
          expect(response.body[0].value).to.equal(order[productTotalEnv])
          expect(response.body[0].ConnectorResponses.authId).to.equal(
            transactionConstants.IMMEDIATECAPTURE
          )
          fn(response)
        })
      })
    })
  })
}

export function verifyPaymentsFinishedStatus(response) {
  expect(response.body[0].status).to.equal(transactionConstants.FINISHED)
}

export function verifyPaymentsCancelledStatus(response) {
  expect(response.body[0].status).to.equal(transactionConstants.CANCELLED)
}

export function verifyPaymentsRefundResponse(response) {
  expect(response.body[0].status).to.equal(transactionConstants.FINISHED)
  const fields = response.body[0].fields.filter(
    field => field.name === transactionConstants.RefundConnectorResponse
  )

  const refundField = response.body[0].fields.filter(
    field => field.name === transactionConstants.RefundConnectorResponse
  )

  expect(fields.length).to.be.greaterThan(0)
  expect(refundField.length).to.be.greaterThan(0)
  expect(fields[0].value).to.contain(transactionConstants.PayPalReport)
}

export function verifyTransactionRefundAPITestCase(transactionIdEnv, fn) {
  it('Verify Transaction Refund', () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        cy.getAPI(
          `${transactionAPI(vtex.baseUrl)}/${order[transactionIdEnv]}/refunds`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(response => {
          expect(response.status).to.equal(200)
          fn(response)
        })
      })
    })
  })
}

export function verifySingleProductTransactionRefundResponse(response) {
  expect(response.body.requests.length).to.be.greaterThan(0)
  expect(response.body.actions.length).to.be.greaterThan(0)
  cy.log('Transaction Refund', response)
}

export function verifyFullRefundTransactionRefundResponse(response) {
  expect(response.body.requests.length).to.be.greaterThan(0)
  expect(response.body.actions.length).to.be.greaterThan(0)
  expect(response.body.actions[0].type).to.equal(transactionConstants.ONLINE)
  expect(response.body.actions[0].value).to.equal(108500)
  cy.log('Transaction Refund', response)
}

export function verifyPartialRefundTransactionRefundResponse(response) {
  expect(response.body.requests.length).to.be.greaterThan(0)
  expect(response.body.actions.length).to.be.greaterThan(0)
  expect(response.body.actions[0].type).to.equal(transactionConstants.ONLINE)
}

export function verifyTransactionCancellationsAPITestCase(transactionIdEnv) {
  it('Verify Transaction Cancellation', () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        cy.getAPI(
          `${transactionAPI(vtex.baseUrl)}/${
            order[transactionIdEnv]
          }/cancellations`,
          VTEX_AUTH_HEADER(vtex.apiKey, vtex.apiToken)
        ).then(response => {
          expect(response.status).to.equal(200)
          cy.log('Transaction Cancellation', response)
        })
      })
    })
  })
}

export function verifyPayPalTransactionAPITestCase(
  { product, transactionIdEnv, productTotalEnv },
  fn
) {
  it('Verify PayPal Transaction', updateRetry(2), () => {
    cy.getVtexItems().then(vtex => {
      cy.getOrderItems().then(order => {
        const orderId = order[generatetidEnv(transactionIdEnv)]

        checkTransactionIdIsAvailable(orderId)
        cy.getAPI(
          `https://api-m.sandbox.paypal.com/v2/checkout/orders/${orderId}`,
          {
            'X-Vtex-Use-Https': true,
            'PayPal-Auth-Assertion': vtex.paypalAuthAssertion,
            Authorization: vtex.paypalAuthorization,
          }
        ).then(response => {
          const amountStr = `${order[productTotalEnv]}`

          expect(response.status).to.equal(200)
          expect(response.body.status).to.equal(transactionConstants.COMPLETED)
          expect(
            response.body.purchase_units[0].payments.captures[0].amount.value
          ).to.equal(`${amountStr.slice(0, -2)}.${amountStr.slice(-2)}`)
          expect(
            response.body.purchase_units[0].shipping.address.postal_code
          ).to.equal(product.postalCode)
          expect(response.body.intent).to.equal(transactionConstants.INTENT)
          expect(response.body.purchase_units[0].items[0].quantity).to.equal(
            product.productQuantity
          )
          expect(
            response.body.purchase_units[0].shipping.name.full_name
          ).to.equal(transactionConstants.NAME)
          fn(response, product)
        })
      })
    })
  })
}

export function verifyProductTransactionResponse(response, product) {
  expect(response.body.purchase_units[0].items[0].unit_amount.value).to.equal(
    product.productPrice
  )
}

export function verifyProductOneTransactionResponse(response, product) {
  expect(response.body.purchase_units[0].items[0].unit_amount.value).to.equal(
    product.product1Price
  )
}

export function verifyRefundPayPalTransactionResponse(response) {
  expect(response.body.purchase_units[0].payments.captures[0].status).to.equal(
    transactionConstants.REFUNDED
  )
}
