﻿// Copyright © 2015-2018 Alex Kukhtin. All rights reserved.

// 20181022-7325
// components/periodpicker.js


(function () {

	const popup = require('std:popup');

	const utils = require('std:utils');
	const eventBus = require('std:eventBus');
	const uPeriod = require('std:period');

	const baseControl = component('control');
	const locale = window.$$locale;

	const DEFAULT_DEBOUNCE = 150;

	Vue.component('a2-period-picker', {
		extends: baseControl,
		template: `
<div class="control-group period-picker" @click.stop.prevent="toggle($event)">
	<label v-if="hasLabel" v-text="label" />
	<div class="input-group">
		<span class="period-text" v-text="text" :class="inputClass" :tabindex="tabIndex"/>
		<span class="caret"/>
		<div class="calendar period-pane" v-if="isOpen" @click.stop.prevent="dummy">
			<ul class="period-menu" style="grid-area: 1 / 1 / span 2 / auto">
				<li v-for='(mi, ix) in menu' :key="ix" :class="{active: isSelectedMenu(mi.key)}"><a v-text='mi.name' @click.stop.prevent="selectMenu(mi.key)"></a></li>
			</ul>
			<a2-calendar style="grid-area: 1 / 2" :show-today="false" pos="left" :model="prevModelDate" 
				:set-month="setMonth" :set-day="setDay" :get-day-class="dayClass" :hover="mouseHover"/>
			<a2-calendar style="grid-area: 1 / 3; margin-left:6px":show-today="false" pos="right" :model="modelDate" 
				:set-month="setMonth" :set-day="setDay" :get-day-class="dayClass" :hover="mouseHover"/>
			<div class="period-footer" style="grid-area: 2 / 2 / auto / span 2">
				<span class="current-period" v-text="currentText" :class="{processing: selection}"/>
				<span class="aligner"></span>
				<button class="btn btn-primary" @click.stop.prevent="apply" v-text="locale.$Apply" :disabled="applyDisabled"/>
				<button class="btn btn-default" @click.stop.prevent="close" v-text="locale.$Cancel" />
			</div>
		</div>
	</div>
	<span class="descr" v-if="hasDescr" v-text="description"></span>
</div>
`,
		props: {
			item: Object,
			prop: String,
			showAll: {
				type: Boolean,
				default: true
			},
			display: String
		},
		data() {
			return {
				isOpen: false,
				selection: '',
				modelDate: utils.date.today(),
				currentPeriod: uPeriod.zero(),
				selectEnd: utils.date.zero(),
				timerId: null
			};
		},
		computed: {
			locale() {
				return window.$$locale;
			},
			text() {
				if (this.display === 'name')
					return this.period.text();
				else if (this.display === 'namedate')
					return `${this.period.text(true)} [${this.period.format('Date')}]`;
				return this.period.format('Date');
			},
			period() {
				let period = this.item[this.prop];
				if (!uPeriod.isPeriod(period))
					console.error('PeriodPicker. Value is not a Period');
				return period;
			},
			prevModelDate() {
				return utils.date.add(this.modelDate, -1, 'month');
			},
			currentText() {
				return this.currentPeriod.format('Date');
			},
			applyDisabled() {
				return this.selection === 'start';
			},
			menu() {
				return uPeriod.predefined(this.showAll);
			}
		},
		methods: {
			dummy() {
			},
			setMonth(d, pos) {
				if (pos === 'left')
					this.modelDate = utils.date.add(d, 1, 'month'); // prev month
				else
					this.modelDate = d;
			},
			setDay(d) {
				if (!this.selection) {
					this.selection = 'start';
					this.currentPeriod.From = d;
					this.currentPeriod.To = utils.date.zero();
					this.selectEnd = utils.date.zero();
				} else if (this.selection === 'start') {
					this.currentPeriod.To = d;
					this.currentPeriod.normalize();
					this.selection = '';
				}
			},
			dayClass(day) {
				let cls = '';
				let px = this.currentPeriod;
				if (this.selection)
					px = uPeriod.create('custom', this.currentPeriod.From, this.selectEnd);
				if (px.in(day))
					cls += ' active';
				if (px.From.getTime() === day.getTime())
					cls += ' period-start';
				if (px.To.getTime() === day.getTime())
					cls += ' period-end';
				return cls;
			},
			close() {
				this.isOpen = false;
			},
			apply() {
				// apply period here
				if (!this.period.equal(this.currentPeriod)) {
					this.period.assign(this.currentPeriod);
					this.fireEvent();
				}
				this.isOpen = false;
			},
			fireEvent() {
				let root = this.item.$root;
				if (!root) return;
				let eventName = this.item._path_ + '.' + this.prop + '.change';
				root.$emit(eventName, this.item, this.period, null);
			},
			toggle(ev) {
				if (!this.isOpen) {
					// close other popups
					eventBus.$emit('closeAllPopups');
					this.modelDate = this.period.To; // TODO: calc start month
					if (this.modelDate.isZero() || this.modelDate.getTime() === utils.date.maxDate.getTime())
						this.modelDate = utils.date.today();
					this.currentPeriod.assign(this.period);
					this.selection = '';
					this.selectEnd = utils.date.zero();
				}
				this.isOpen = !this.isOpen;
			},
			__clickOutside() {
				this.isOpen = false;
			},
			isSelectedMenu(key) {
				let p = uPeriod.create(key);
				return p.equal(this.currentPeriod);
			},
			selectMenu(key) {
				let p = uPeriod.create(key);
				this.currentPeriod.assign(p);
				this.apply();
			},
			mouseHover(day) {
				if (!this.selection) return;
				if (this.selectEnd.getTime() === day.getTime()) return;
				clearTimeout(this.timerId);
				this.timerId = setTimeout(() => {
					this.selectEnd = day;
				}, DEFAULT_DEBOUNCE);
			}
		},
		mounted() {
			popup.registerPopup(this.$el);
			this.$el._close = this.__clickOutside;
		},
		beforeDestroy() {
			popup.unregisterPopup(this.$el);
		}
	});
})();

