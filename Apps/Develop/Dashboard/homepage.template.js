﻿
/* index template */

const template = {
	properties: {
	},
	methods: {
	},
	delegates: {
		drawChart
	},
	events: {
	},
	validators: {
	},
	commands: {
	}
};

module.exports = template;

function drawChart(g) {
	const chart = g.append('svg')
		.attr('viewBox', `0 0 300 50`);

	const sine = d3.range(0, 50)
		.map(k => {
			let x = .5 * k * Math.PI;
			return [x, Math.sin(x / 4)];
		});

	// scale functions
	const x = d3.scaleLinear()
		.range([0, 300])
		.domain(d3.extent(sine, d => d[0]));

	const y = d3.scaleLinear()
		.range([50, 0])
		.domain([-1, 1]);

	// line generator
	const area = d3.area()
		.x(d => x(d[0]))
		.y0(50)
		.y1(d => y(d[1]));

	chart.append('g')
		.append('path')
		.datum(sine)
		.attr('fill', "hsla(0,0%,70%,.25)")
		.attr('d', area);
		//.attr('stroke', '#fff')
}