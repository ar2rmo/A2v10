﻿/* platform mock */

(function () {

    function set(target, prop, value) {
        target[prop] = value;
    }


	app.modules['std:platform'] = {
		set: set,
		File: File, /*file ctor*/
		performance: performance
	};

})();