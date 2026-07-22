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

function MoneizDownloadFile(name, content) {
  const contentType = "application/octet-stream";
  
  // Create the URL
  const file = new File([content], name, { type: contentType });
  const exportUrl = URL.createObjectURL(file);

  // Create the <a> element and click on it
  const a = document.createElement("a");
  document.body.appendChild(a);
  a.href = exportUrl;
  a.download = name;
  a.target = "_self";
  a.click();
  URL.revokeObjectURL(exportUrl);
}

async function MoneizUpdate() {
  if (!("serviceWorker" in navigator)) {
    location.reload();
    return;
  }

  const serviceWorker = await navigator.serviceWorker.ready;
  await serviceWorker.update();
  if (serviceWorker.waiting) {
    const controllerChange = new Promise(resolve => {
      navigator.serviceWorker.addEventListener("controllerchange", resolve, { once: true });
    });

    serviceWorker.waiting.postMessage({ type: "SKIP_WAITING" });
    await controllerChange;
  }

  location.reload();
}

async function MoneizHasUpdateAvailable() {
  if (!("serviceWorker" in navigator)) {
    return false;
  }

  const registration = await navigator.serviceWorker.getRegistration();
  if (!registration) {
    return false;
  }

  await registration.update();
  return !!registration.waiting;
}

function MoneizTrapEnterKey(element) {
  if (!element || element.moneizTrapEnterKey) {
    return;
  }

  element.moneizTrapEnterKey = function (event) {
    if (event.key === "Enter" && element.getAttribute("aria-expanded") === "true" && element.getAttribute("aria-activedescendant")) {
      event.preventDefault();
    }
  };

  element.addEventListener("keydown", element.moneizTrapEnterKey);
}

function MoneizReleaseTrapEnterKey(element) {
  if (!element || !element.moneizTrapEnterKey) {
    return;
  }

  element.removeEventListener("keydown", element.moneizTrapEnterKey);
  delete element.moneizTrapEnterKey;
}

function MoneizGetBoundingClientRect(element) {
    const rect = element.getBoundingClientRect();
    return { bottom: rect.bottom, left: rect.left, width: rect.width };
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
