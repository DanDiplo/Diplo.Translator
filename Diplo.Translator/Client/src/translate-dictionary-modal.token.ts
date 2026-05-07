import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import { TRANSLATOR_MODAL_ALIAS } from "./constants";

export interface DiploTranslatorModalData {
  onComplete?: () => void | Promise<void>;
}

export const DIPLO_TRANSLATOR_MODAL = new UmbModalToken<DiploTranslatorModalData, never>(TRANSLATOR_MODAL_ALIAS, {
  modal: {
    type: "sidebar",
    size: "small"
  }
});
