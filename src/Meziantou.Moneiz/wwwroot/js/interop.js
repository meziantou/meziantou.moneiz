const MoneizLocalStorageDbName = "moneiz.db";
const MoneizLocalStorageChangedName = "moneiz.dbchanged";
const MoneizDbFileName = "moneiz.moneizdb";

async function MoneizGetValue(name) {
  return localforage.getItem(name);
}

async function MoneizSetValue(name, value) {
  await localforage.setItem(name, value);
}

async function MoneizDownloadFile(name, content) {
  const data = base64DecToArr(content);
  const file = new File([data], name, { type: "application/octet-stream" });
  const exportUrl = URL.createObjectURL(file);

  const a = document.createElement("a");
  document.body.appendChild(a);
  a.href = exportUrl;
  a.download = name;
  a.target = "_self";
  a.click();
  //URL.revokeObjectURL(exportUrl); doesn't work with Safari

  function b64ToUint6(nChr) {

    return nChr > 64 && nChr < 91 ?
      nChr - 65
      : nChr > 96 && nChr < 123 ?
        nChr - 71
        : nChr > 47 && nChr < 58 ?
          nChr + 4
          : nChr === 43 ?
            62
            : nChr === 47 ?
              63
              :
              0;

  }

  function base64DecToArr(sBase64, nBlocksSize) {

    var
      sB64Enc = sBase64.replace(/[^A-Za-z0-9\+\/]/g, ""), nInLen = sB64Enc.length,
      nOutLen = nBlocksSize ? Math.ceil((nInLen * 3 + 1 >> 2) / nBlocksSize) * nBlocksSize : nInLen * 3 + 1 >> 2, taBytes = new Uint8Array(nOutLen);

    for (var nMod3, nMod4, nUint24 = 0, nOutIdx = 0, nInIdx = 0; nInIdx < nInLen; nInIdx++) {
      nMod4 = nInIdx & 3;
      nUint24 |= b64ToUint6(sB64Enc.charCodeAt(nInIdx)) << 18 - 6 * nMod4;
      if (nMod4 === 3 || nInLen - nInIdx === 1) {
        for (nMod3 = 0; nMod3 < 3 && nOutIdx < nOutLen; nMod3++, nOutIdx++) {
          taBytes[nOutIdx] = nUint24 >>> (16 >>> nMod3 & 24) & 255;
        }
        nUint24 = 0;

      }
    }

    return taBytes;
  }
}

function MoneizConfirm(message) {
  return confirm(message);
}

function MoneizAlert(message) {
  return alert(message);
}

function MoneizOpenInTab(url) {
  window.open(url, '_blank');
}

(function () {
  let state = false;

  window.addEventListener("beforeunload", function (e) {
    if (state) {
      e.returnValue = "You have unsaved changes. Do you want to exit before exporting your data?";
    }
  });

  setInterval(async () => {
    const value = await MoneizGetValue(MoneizLocalStorageChangedName);
    if (value) {
      state = JSON.parse(value);
    } else {
      state = false;
    }
  }, 500);
})()
