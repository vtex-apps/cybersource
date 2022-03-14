#!/bin/bash

# Set files and binaries
VTEX_PATH="$HOME/.cache"
VTEX_ENV="$HOME/.vtex"
CY_CACHE="$HOME/.config/Cypress/cy/"
VTEX_BIN="$VTEX_PATH/vtex-e2e"
VTEX_URL=".vtex.url"

# Set env vars
export IN_CYPRESS=true
export PATH=$PATH:$VTEX_PATH
# TODO: Move the next line to CI
export CYPRESS_VTEX_ACCOUNT="sandboxusdev"

# Setup VTEX CLI
rm -rf $VTEX_ENV
if [[ -f $VTEX_BIN ]]; then
  echo "[QE] ===> Toolbelt found, using it!"
  echo "[QE] ===> Removing old $VTEX_URL if any..."
  rm -f $VTEX_URL &>/dev/null
  echo "[QE] ===> Removing $VTEX_ENV folder if any..."
  rm -rf $VTEX_ENV &>/dev/null
  echo "[QE] ===> Running 'vtex whoami'..."
  # It's called twice because of a bug on MacOS
  sleep 2 && $VTEX_BIN whoami &>/dev/null
  sleep 2 && $VTEX_BIN whoami &>/dev/null
else
  # Set up the toolbelt patched
  echo "[QE] ===> Toolbelt not found, deploying it..."
  CURRENT_DIR=$(pwd)
  echo "[QE] ===> Changing current dir to $VTEX_PATH"
  cd $VTEX_PATH && rm -rf toolbelt &>/dev/null
  echo "[QE] ===> Cloning the toolbelt git repository..."
  git clone https://github.com/vtex/toolbelt.git &> /dev/null
  echo "[QE] ===> Changing the branch to patched one (qe/cypress)..."
  cd toolbelt && git checkout qe/cypress &> /dev/null
  echo "[QE] ===> Installing toolbelt packages..."
  yarn &> /dev/null
  yarn build &> /dev/null
  echo "[QE] ===> Calling the VTEX CLI to warm it up..."
  ln -s $VTEX_PATH/toolbelt/bin/run $VTEX_BIN
  # twice because of a vtex cli bug
  timeout 5 $VTEX_BIN whoami &> /dev/null
  timeout 5 $VTEX_BIN whoami &> /dev/null
  echo "[QE] ===> Changing back the dir to $CURRENT_DIR..."
  cd $CURRENT_DIR
fi

echo "[QE] ===> Calling VTEX CLI in background..."
$VTEX_BIN login $CYPRESS_VTEX_ACCOUNT 1>$VTEX_URL &

echo "[QE] ===> Waiting for $VTEX_URL be updated..."
SIZE=0
until [[ $SIZE -ge 3 ]]; do
  [[ -f $VTEX_URL ]] && SIZE=$(du $VTEX_URL | cut -f1)
  sleep 1
done

echo "[QE] ===> Cleaning any previous cache..."
echo "{}" > .vtex.json
echo "{}" > .orders.json
rm -rf $CY_CACHE &> /dev/null

echo "[QE] ===> Set up done, now you can call Cypress."
