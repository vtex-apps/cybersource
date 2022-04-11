const cybersourceRestApi = require('cybersource-rest-client')

const AuthenticationType = 'http_signature'
const RunEnvironment = 'apitest.cybersource.com'

// jwt parameters
const KeysDirectory = 'Resource'
const KeyFileName = 'testrest'
const KeyAlias = 'testrest'
const KeyPass = 'testrest'

// meta key parameters
const UseMetaKey = false
const PortfolioID = ''

// logging parameters
const EnableLog = true
const LogFileName = 'cybs'
const LogDirectory = 'log'
const LogfileMaxSize = '5242880' // 10 MB In Bytes
const EnableMasking = true

function retrieveTransactions(vtex, id, transactionscallback) {
  const configObject = {
    authenticationType: AuthenticationType,
    runEnvironment: RunEnvironment,

    merchantID: vtex.merchantId,
    // http_signature parameters
    merchantKeyId: vtex.merchantKeyId,
    merchantsecretKey: vtex.merchantSharedKey,

    keyAlias: KeyAlias,
    keyPass: KeyPass,
    keyFileName: KeyFileName,
    keysDirectory: KeysDirectory,

    useMetaKey: UseMetaKey,
    portfolioID: PortfolioID,

    logConfiguration: {
      enableLog: EnableLog,
      logFileName: LogFileName,
      logDirectory: LogDirectory,
      logFileMaxSize: LogfileMaxSize,
      loggingLevel: 'debug',
      enableMasking: EnableMasking,
    },
  }

  const apiClient = new cybersourceRestApi.ApiClient()
  const instance = new cybersourceRestApi.TransactionDetailsApi(
    configObject,
    apiClient
  )

  instance.getTransaction(id, (error, data, response) => {
    transactionscallback(error, data, response)
  })
}

module.exports = { retrieveTransactions }
