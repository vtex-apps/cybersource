export default {
  AdminLogin: (apiKey, apiToken) => {
    return `https://vtexid.vtex.com.br/api/vtexid/pub/authenticate/default?user=${apiKey}&pass=${apiToken}`
  },
  invoiceAPI: baseUrl => {
    return `${baseUrl}/api/oms/pvt/orders`
  },
  transactionAPI: (baseUrl, id) => {
    return `${baseUrl}/api/payments/pvt/transactions/${id}`
  },
}
