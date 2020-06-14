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
  const file = new File([content], name, { type: "application/octet-stream" });
  const exportUrl = URL.createObjectURL(file);

  const a = document.createElement("a");
  document.body.appendChild(a);
  a.href = exportUrl;
  a.download = name;
  a.target = "_self";
  a.click();
  //URL.revokeObjectURL(exportUrl); doesn't work with Safari
}

function MoneizConfirm(message) {
  return confirm(message);
}

function MoneizAlert(message) {
  return alert(message);
}
