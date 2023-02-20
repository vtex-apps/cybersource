import { graphql } from './common/graphql_utils.js'

export const ORDER_SUFFIX = '-testing'

const APP = 'vtex.apps-graphql@2.x'

export function getAppSettings() {
  return {
    query: 'query' + '{appSettings(app:"vtex.cybersource-ui"){message}}',
    queryVariables: {},
  }
}

export function saveAppSettings(settings) {
  return {
    query:
      'mutation($app:String,$settings:String)' +
      `{saveAppSettings(app:$app,settings:$settings){message}}`,
    queryVariables: {
      app: 'vtex.cybersource-ui',
      settings: JSON.stringify(settings),
    },
  }
}

export function updateCybersourceConfiguration(orderSuffix = '') {
  it('Update cybersource app settings', () => {
    graphql(APP, getAppSettings(), ({ body }) => {
      const { message } = body.data.appSettings
      const jsonMessage = JSON.parse(message)

      jsonMessage.OrderSuffix = orderSuffix
      graphql(APP, saveAppSettings(jsonMessage), response => {
        const { OrderSuffix } = JSON.parse(
          response.body.data.saveAppSettings.message
        )

        expect(OrderSuffix).to.contain(orderSuffix)
      })
    })
  })
}
