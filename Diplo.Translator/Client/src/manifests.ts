import { UMB_DICTIONARY_ENTITY_TYPE, UMB_DICTIONARY_ROOT_ENTITY_TYPE } from "@umbraco-cms/backoffice/dictionary";
import { TRANSLATOR_MODAL_ALIAS } from "./constants";

export const manifests: Array<UmbExtensionManifest> = [
  {
    type: "entityAction",
    kind: "default",
    alias: "Diplo.Translator.EntityAction.TranslateDictionary",
    name: "Translate Dictionary Entity Action",
    weight: 350,
    api: () => import("./translate-dictionary.action"),
    forEntityTypes: [UMB_DICTIONARY_ENTITY_TYPE, UMB_DICTIONARY_ROOT_ENTITY_TYPE],
    meta: {
      icon: "icon-globe",
      label: "Translate",
      additionalOptions: true
    }
  },
  {
    type: "modal",
    alias: TRANSLATOR_MODAL_ALIAS,
    name: "Translate Dictionary Modal",
    element: () => import("./translate-dictionary-modal.element")
  }
];
