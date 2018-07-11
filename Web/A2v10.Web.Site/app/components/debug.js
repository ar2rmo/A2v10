﻿// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

// 20180619-7227
// components/debug.js*/

(function () {

    /**
     * TODO
    1. Trace window
    2. Dock right/left
    6.
     */

	const dataService = require('std:dataservice');
	const urlTools = require('std:url');
	const eventBus = require('std:eventBus');
	const locale = window.$$locale;
	const utils = require('std:utils');

	const isZero = utils.date.isZero;

	const specKeys = {
		'$vm': null,
		'$host': null,
		'$root': null,
		'$parent': null
	};

	function toJsonDebug(data) {
		return JSON.stringify(data, function (key, value) {
			if (key[0] === '$')
				return !(key in specKeys) ? value : undefined;
			else if (key[0] === '_')
				return undefined;
			if (isZero(this[key])) return null;
			return value;
		}, 2);
	}

	const traceItem = {
		name: 'a2-trace-item',
		template: `
<div v-if="hasElem" class="trace-item-body">
    <span class="title" v-text="name"/><span class="badge" v-text="elem.length"/>
    <ul class="a2-debug-trace-item">
        <li v-for="itm in elem">
            
            <div class="rq-title"><span class="elapsed" v-text="itm.elapsed + ' ms'"/> <span v-text="itm.text"/></div>
        </li>
    </ul>
</div>
`,
		props: {
			name: String,
			elem: Array
		},
		computed: {
			hasElem() {
				return this.elem && this.elem.length;
			}
		}
	};

	Vue.component('a2-debug', {
		template: `
<div class="debug-panel" v-if="paneVisible">
    <div class="debug-pane-header">
        <span class="debug-pane-title" v-text="title"></span>
        <a class="btn btn-close" @click.prevent="close">&#x2715</a>
    </div>
    <div class="toolbar">
        <button class="btn btn-tb" @click.prevent="refresh"><i class="ico ico-reload"></i> {{text('$Refresh')}}</button>
    </div>
    <div class="debug-model debug-body" v-if="modelVisible">
        <pre class="a2-code" v-text="modelJson()"></pre>
    </div>
    <div class="debug-trace debug-body" v-if="traceVisible">
        <ul class="a2-debug-trace">
            <li v-for="r in trace">
                <div class="rq-title"><span class="elapsed" v-text="r.elapsed + ' ms'"/> <span v-text="r.url" /></div>
                <a2-trace-item name="Sql" :elem="r.items.Sql"></a2-trace-item>
                <a2-trace-item name="Render" :elem="r.items.Render"></a2-trace-item>
                <a2-trace-item name="Workflow" :elem="r.items.Workflow"></a2-trace-item>
                <a2-trace-item class="exception" name="Exceptions" :elem="r.items.Exception"></a2-trace-item>
            </li>
        </ul>
    </div>
</div>
`,
		components: {
			'a2-trace-item': traceItem
		},
		props: {
			modelVisible: Boolean,
			traceVisible: Boolean,
			modelStack: Array,
			counter: Number,
			close: Function
		},
		data() {
			return {
				trace: []
			};
		},
		computed: {
			refreshCount() {
				return this.counter;
			},
			paneVisible() {
				return this.modelVisible || this.traceVisible;
			},
			title() {
				return this.modelVisible ? locale.$DataModel
					: this.traceVisible ? locale.$Profiling
						: '';
			},
			traceView() {
				return this.traceVisible;
			}
		},
		methods: {
			modelJson() {
				// method. not cached
				if (!this.modelVisible)
					return;
				if (this.modelStack.length) {
					return toJsonDebug(this.modelStack[0].$data);
				}
				return '';
			},
			refresh() {
				if (this.modelVisible)
					this.$forceUpdate();
				else if (this.traceVisible)
					this.loadTrace();
			},
			loadTrace() {
				const root = window.$$rootUrl;
				const url = urlTools.combine(root, 'shell/trace');
				const that = this;
				dataService.post(url).then(function (result) {
					that.trace.splice(0, that.trace.length);
					if (!result) return;
					result.forEach((val) => {
						that.trace.push(val);
					});
				});
			},
			text(key) {
				return locale[key];
			}
		},
		watch: {
			refreshCount() {
				// dataModel stack changed
				this.$forceUpdate();
			},
			traceView(newVal) {
				if (newVal)
					this.loadTrace();
			}
		},
		created() {
			eventBus.$on('endRequest', (url) => {
				if (url.indexOf('/shell/trace') !== -1) return;
				if (!this.traceVisible) return;
				this.loadTrace();
			});
		}
	});
})();
