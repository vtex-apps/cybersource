## On GitHub Actions

To run the Cypres on GitHub Actions you need to create a SECRET called `VTEX_CYPRESS` with the following JSON:

```
{
    "VTEX_ACCOUNT": "",
    "VTEX_ACCOUNT_ID": "",
    "VTEX_API_KEY": "",
    "VTEX_API_TOKEN": "",
    "VTEX_ROBOT_MAIL": "",
    "VTEX_ROBOT_PASSWORD": "",
    "VTEX_AVALARA_AUTHORIZATION": "",
    "VTEX_AUTHORIZATION": "",
    "TWILIO_USER": "",
    "TWILIO_TOKEN": "",
    "VTEX_EXTERNAL_SELLER_URL": "",
    "VTEX_JIRA_ACCOUNT": "",
    "VTEX_JIRA_AUTHORIZATION": "",
    "VTEX_JIRA_KEY": "",
    "VTEX_JIRA_ASSIGNEE_ID": "",
    "VTEX_CREATE_TICKET": false
}

```

With the SECRET configured, create the follow GitHub Action:

```
# This is a basic workflow that is manually triggered

name: '[QE] Cypress'

on:
  pull_request:
    branches:
      - master
      - main

concurrency:
  group: ${{ github.workflow }}
  cancel-in-progress: false

jobs:
  cypress:
    runs-on: ubuntu-latest
    steps:
    - name: Install Chrome
      uses: browser-actions/setup-chrome@latest
    - name: Check Chrome version
      run: chrome --version
    - name: Checkout
      uses: actions/checkout@v2
      with:
        fetch-depth: 0  # Shallow clones should be disabled for a better relevancy of analysis
        ref: ${{ github.event.pull_request.head.sha }}
    - name: Cache YARN
      uses: actions/cache@v2
      with:
        path: '~/.cache/'
        key: ${{ runner.os }}-modules-${{ hashFiles('**/yarn.lock') }}
    - name: Install dependencies
      run: yarn install --frozen-lockfile
      if: steps.cache-node-modules.outputs.cache-hit != 'true'
    - name: Start E2E
      run: node cypress/cypress.js
      if: steps.cache-node-modules.outputs.cache-hit != 'true'
      env:
        # CYPRESS_RECORD_KEY: ${{ secrets.DASHBOARD_CYPRESS }}
        VTEX_CYPRESS: ${{ secrets.VTEX_CYPRESS }}
    - name: Save screenshots
      uses: actions/upload-artifact@master
      if: always()
      with:
        name: screenshots
        path: cypress/screenshots
```

## On local machine

1. You need to create a file called `secrets.env.json` with the following content:

```
{
    "VTEX_ACCOUNT": "",
    "VTEX_ACCOUNT_ID": "",
    "VTEX_API_KEY": "",
    "VTEX_API_TOKEN": "",
    "VTEX_ROBOT_MAIL": "",
    "VTEX_ROBOT_PASSWORD": "",
    "VTEX_AVALARA_AUTHORIZATION": "",
    "VTEX_AUTHORIZATION": "",
    "TWILIO_USER": "",
    "TWILIO_TOKEN": "",
    "VTEX_EXTERNAL_SELLER_URL": "",
    "VTEX_JIRA_ACCOUNT": "",
    "VTEX_JIRA_AUTHORIZATION": "",
    "VTEX_JIRA_KEY": "",
    "VTEX_JIRA_ASSIGNEE_ID": "",
    "VTEX_CREATE_TICKET": false
}

```

2. Next you must export it as a local variable

```
export VTEX_CYPRESS=$(cat secrets.env.json)
```

3. Install local dependencies

```
yarn install
```

4. Call the `cypress.js` script using `node`

```
# To run using the dynamic workspace in background mode
node cypress/cypress.js

# To run using the DEV workspace in background mode
node cypress/cypress.js --dev

# To run using the DEV workspace in foreground mode
node cypress/cypress.js --dev --show

# To open using the dynamic workspace
node cypress/cypress.js --open

# To open and bypass the VTEX CLI login (in this case, the last workspace credentials will be used instead)
node cypress/cypress.js --no-vtex --open

```

5. You can combine the flags the way best pleases you.
