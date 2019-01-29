﻿/*invoice template*/

const cmn = require('document/common');
const utils = require('std:utils');

const template = {
	properties: {
		'TRow.Sum': cmn.rowSum,
		'TDocument.Sum': cmn.docTotalSum,
		//'TDocument.$DatePlusOne'() { return utils.date.add(this.Date, -2, 'day');},
		'TDocument.$canShipment': canShipment,
		'TDocument.$checked': Boolean,
		'TDocLink.$Mark'() { return this.Done ? 'success' : null; },
		'TRow.$RoundSum'() { return utils.currency.round(this.Price * this.Qty, 3);}
	},
	validators: {
		'Document.Agent': 'Выберите покупателя',
		'Document.Rows[].Entity': 'Выберите товар',
		'Document.Rows[].Price': 'Укажите цену'
	},
	events: {
		'Model.load': modelLoad,
		'Model.saved'(root) { console.dir(root);},
		'Document.Rows[].add': (arr, row) => row.Qty = 1,
		'Document.Rows[].Entity.Article.change': cmn.findArticle,
		'Document.Agent.change': (doc) => { console.dir('Agent.change'); },
		'Document.Date.change': (doc, newVal, oldVal) => { console.dir(`Date.change nv:${newVal}, ov:${oldVal}`); }
	},
	commands: {
		apply: cmn.docApply,
		unApply: cmn.docUnApply,
		createShipment,
		createPayment,
		createNewCustomer
	},
	delegates: {
		fetchCustomers
	}
};

module.exports = template;

async function modelLoad(root, caller) {
	if (root.Document.$isNew)
		cmn.documentCreate(root.Document, 'Invoice');
	/*
	const ctrl = this.$ctrl;
	let x = await ctrl.$invoke('testCommand', { Text: '%' });
	console.dir(x);
	let y = await ctrl.$invoke('testCommand', {Text:'AAA'});
	console.dir(y);
	let z = await ctrl.$invoke('testCommand', { Text: 'CCC' });
	console.dir(z);
	*/
	/*
	ctrl.$defer(async () => {
		await Promise.all([x, y]);
		console.dir(x);
	});
	*/
}

async function createShipment(doc) {
	const vm = doc.$vm;
	let result = await vm.$invoke('createShipment', { Id: doc.Id });
	if (result.Document) {
		vm.$navigate('/sales/waybill/edit', result.Document.Id);
        /*
        А можно не открывать, а просто показать
        doc.Shipment.$append(result.Document);
        */
	}
}

async function createPayment(doc) {
	const vm = doc.$vm;
	vm.$alert('Пока не реализовано');
	//let result = await vm.$invoke('createPayment', { Id: doc.Id });
	//vm.$navigate('/document/payment/edit', result.Document.Id)
}

function canShipment() {
	return this.Shipment.Count === 0;
}

function fetchCustomers(agent, text) {
	var vm = this.$vm;
	return vm.$invoke('fetchCustomer', { Text: text, Kind: 'Customer' });
}

async function createNewCustomer(text) {
	var vm = this.$vm;
	var cust = await vm.$showDialog('/Agent/editCustomer', 0, { Name: text });
	console.warn('before set');
	vm.Document.Agent = cust;
	console.warn('end set');
}
