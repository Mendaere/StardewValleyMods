﻿using System.Collections.Generic;
using System.Linq;
using TehPers.CoreMod.Api;
using TehPers.CoreMod.Api.ContentLoading;

namespace TehPers.CoreMod.ContentLoading {
    internal class CoreTranslationHelper : ICoreTranslationHelper {
        private readonly ICoreApi _coreApi;

        public CoreTranslationHelper(ICoreApi coreApi) {
            this._coreApi = coreApi;
        }

        public ICoreTranslation Get(string key) {
            return new CoreTranslation(this._coreApi.Owner.Helper.Translation.Get(key));
        }

        public IEnumerable<ICoreTranslation> GetAll() {
            return this._coreApi.Owner.Helper.Translation.GetTranslations().Select(t => new CoreTranslation(t));
        }
    }
}