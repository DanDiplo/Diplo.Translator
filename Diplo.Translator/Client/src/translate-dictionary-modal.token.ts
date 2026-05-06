import { UmbModalToken } from "@umbraco-cms/backoffice/modal";
import { TRANSLATOR_MODAL_ALIAS } from "./constants";

export const DIPLO_TRANSLATOR_MODAL = new UmbModalToken<never, never>(TRANSLATOR_MODAL_ALIAS, {
  modal: {
    type: "sidebar",
    size: "small"
  }
});
