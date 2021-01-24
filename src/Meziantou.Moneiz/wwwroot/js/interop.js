const MoneizLocalStorageChangedName = "moneiz.dbchanged";

async function MoneizGetValue(name) {
  return localforage.getItem(name);
}

async function MoneizSetValue(name, value) {
  if (navigator.storage && navigator.storage.persist) {
    const isPersisted = await navigator.storage.persist();
    console.log(`Persisted storage granted: ${isPersisted}`);
  }

  await localforage.setItem(name, value);
}

async function MoneizDownloadFile(name, content) {
  // Convert the parameters to actual JS types
  const nameStr = BINDING.conv_string(name);
  const contentTypeStr = "application/octet-stream";
  const contentArray = Blazor.platform.toUint8Array(content);

  // Create the URL
  const file = new File([contentArray], nameStr, { type: contentTypeStr });
  const exportUrl = URL.createObjectURL(file);

  // Create the <a> element and click on it
  const a = document.createElement("a");
  document.body.appendChild(a);
  a.href = exportUrl;
  a.download = nameStr;
  a.target = "_self";
  a.click();
  //URL.revokeObjectURL(exportUrl); doesn't work with Safari
}

function MoneizOpenInTab(url) {
  window.open(url, '_blank');
}

async function MoneizUpdate() {
  const serviceWorker = await navigator.serviceWorker.ready;
  await serviceWorker.update();
  await serviceWorker.unregister();
  location.reload(true);
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
