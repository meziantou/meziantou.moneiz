const MoneizLocalStorageDbName = "moneiz.db";
const MoneizLocalStorageChangedName = "moneiz.dbchanged";
const MoneizDbFileName = "moneiz.moneizdb";

async function MoneizDatabaseIsExported() {
  return (await localforage.getItem(MoneizLocalStorageChangedName)) !== true;
}

function MoneizLoadDatabase() {
  return localforage.getItem(MoneizLocalStorageDbName);
}

async function MoneizSaveDatabase(content, options) {
  await localforage.setItem(MoneizLocalStorageDbName, content);
  await localforage.setItem(MoneizLocalStorageChangedName, options.indicateDbChanged);
}

async function MoneizExportDatabase() {
  const content = await MoneizLoadDatabase();

  const file = new File([content], MoneizDbFileName, { type: "application/octet-stream" });
  const exportUrl = URL.createObjectURL(file);

  const a = document.createElement("a");
  document.body.appendChild(a);
  a.href = exportUrl;
  a.download = MoneizDbFileName;
  a.target = "_self";
  a.click();
  //URL.revokeObjectURL(exportUrl); doesn't work with Safari

  await localforage.removeItem(MoneizLocalStorageChangedName);
}

function MoneizConfirm(message) {
  return confirm(message);
}

function MoneizAlert(message) {
  return alert(message);
}
