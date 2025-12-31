// Pure Canvas line chart implementation - zero dependencies
window.MoneizCharts = {
    charts: new WeakMap(),

    createLineChart: function(canvas, labelsJson, datasetsJson, optionsJson) {
        console.log('[Chart] createLineChart called', typeof labelsJson, typeof datasetsJson);
        if (!canvas) {
            console.error('[Chart] Canvas is null');
            return false;
        }

        try {
            // Parse JSON strings
            const labels = JSON.parse(labelsJson);
            const datasets = JSON.parse(datasetsJson);
            const options = JSON.parse(optionsJson);
            console.log('[Chart] Parsed data:', labels.length, 'labels,', datasets.length, 'datasets');

            const ctx = canvas.getContext('2d');
            const lineWidth = options?.lineWidth || 2;

            // Set canvas size to match container
            const rect = canvas.getBoundingClientRect();
            const dpr = window.devicePixelRatio || 1;
            canvas.width = rect.width * dpr;
            canvas.height = rect.height * dpr;
            ctx.scale(dpr, dpr);

            const width = rect.width;
            const height = rect.height;

            // Chart padding and dimensions
            const padding = { top: 60, right: 20, bottom: 50, left: 80 };
            const chartWidth = width - padding.left - padding.right;
            const chartHeight = height - padding.top - padding.bottom;

            // Optimize datasets by removing intermediate points with no change
            const optimizedDatasets = datasets.map(dataset => {
                const optimizedData = [];
                const optimizedIndices = [];

                for (let i = 0; i < dataset.data.length; i++) {
                    // Always keep first and last points
                    if (i === 0 || i === dataset.data.length - 1) {
                        optimizedData.push(dataset.data[i]);
                        optimizedIndices.push(i);
                    } else {
                        // Keep point if value changed from previous
                        if (dataset.data[i] !== dataset.data[i - 1]) {
                            optimizedData.push(dataset.data[i]);
                            optimizedIndices.push(i);
                        }
                    }
                }

                return {
                    label: dataset.label,
                    data: optimizedData,
                    indices: optimizedIndices,
                    originalData: dataset.data
                };
            });

            // Calculate data ranges
            let minValue = Infinity;
            let maxValue = -Infinity;
            datasets.forEach(dataset => {
                dataset.data.forEach(val => {
                    if (val < minValue) minValue = val;
                    if (val > maxValue) maxValue = val;
                });
            });

            // Add some padding to the range
            const valueRange = maxValue - minValue;
            minValue = minValue - valueRange * 0.1;
            maxValue = maxValue + valueRange * 0.1;

            // Helper functions
            const getX = (index) => padding.left + (index / (labels.length - 1)) * chartWidth;
            const getY = (value) => padding.top + chartHeight - ((value - minValue) / (maxValue - minValue)) * chartHeight;

            // Theme colors
            const isDarkTheme = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
            const textColor = isDarkTheme ? '#e5e7eb' : '#1f2937';
            const secondaryTextColor = isDarkTheme ? '#9ca3af' : '#6b7280';
            const gridColor = isDarkTheme ? '#374151' : '#e5e7eb';
            const axisColor = isDarkTheme ? '#9ca3af' : '#1f2937';
            const colors = [
                '#3b82f6', '#ef4444', '#10b981', '#f59e0b', '#8b5cf6',
                '#ec4899', '#14b8a6', '#f97316', '#6366f1', '#84cc16'
            ];

            // Store chart info for interaction
            const chartInfo = {
                canvas,
                ctx,
                datasets: optimizedDatasets,
                labels,
                padding,
                chartWidth,
                chartHeight,
                minValue,
                maxValue,
                getX,
                getY,
                colors,
                lineWidth,
                width,
                height,
                tooltip: null
            };

            this.charts.set(canvas, chartInfo);

            // Add mouse event listeners for interactivity
            canvas.addEventListener('mousemove', (e) => this.handleMouseMove(e, chartInfo));
            canvas.addEventListener('mouseleave', () => this.handleMouseLeave(chartInfo));

            // Initial draw
            ctx.clearRect(0, 0, width, height);

            // Draw grid lines
            ctx.strokeStyle = gridColor;
            ctx.lineWidth = 1;
            ctx.setLineDash([5, 5]);

            const gridLines = 5;
            for (let i = 0; i <= gridLines; i++) {
                const y = padding.top + (i / gridLines) * chartHeight;
                ctx.beginPath();
                ctx.moveTo(padding.left, y);
                ctx.lineTo(padding.left + chartWidth, y);
                ctx.stroke();
            }
            ctx.setLineDash([]);

            // Draw axes
            ctx.strokeStyle = axisColor;
            ctx.lineWidth = 2;
            ctx.beginPath();
            ctx.moveTo(padding.left, padding.top);
            ctx.lineTo(padding.left, padding.top + chartHeight);
            ctx.lineTo(padding.left + chartWidth, padding.top + chartHeight);
            ctx.stroke();

            // Draw Y-axis labels
            ctx.fillStyle = secondaryTextColor;
            ctx.font = '12px sans-serif';
            ctx.textAlign = 'right';
            ctx.textBaseline = 'middle';

            for (let i = 0; i <= gridLines; i++) {
                const value = maxValue - (i / gridLines) * (maxValue - minValue);
                const y = padding.top + (i / gridLines) * chartHeight;
                ctx.fillText(value.toFixed(2), padding.left - 10, y);
            }

            // Draw X-axis labels (smart spacing to avoid overlap)
            ctx.textAlign = 'center';
            ctx.textBaseline = 'top';
            ctx.font = '12px sans-serif';

            // Calculate minimum spacing between labels (estimate ~60px to avoid overlap)
            const minLabelSpacing = 60;
            const maxLabels = Math.floor(chartWidth / minLabelSpacing);
            const labelStep = Math.max(1, Math.ceil(labels.length / maxLabels));

            labels.forEach((label, index) => {
                if (label && index % labelStep === 0) {
                    const x = getX(index);
                    ctx.fillText(label, x, padding.top + chartHeight + 10);
                }
            });

            // Draw datasets
            optimizedDatasets.forEach((dataset, datasetIndex) => {
                const color = colors[datasetIndex % colors.length];

                // Draw line
                ctx.strokeStyle = color;
                ctx.lineWidth = lineWidth;
                ctx.beginPath();

                dataset.indices.forEach((originalIndex, i) => {
                    const x = getX(originalIndex);
                    const y = getY(dataset.data[i]);

                    if (i === 0) {
                        ctx.moveTo(x, y);
                    } else {
                        ctx.lineTo(x, y);
                    }
                });

                ctx.stroke();

                // Draw points
                dataset.indices.forEach((originalIndex, i) => {
                    const x = getX(originalIndex);
                    const y = getY(dataset.data[i]);

                    ctx.fillStyle = color;
                    ctx.beginPath();
                    ctx.arc(x, y, 3, 0, 2 * Math.PI);
                    ctx.fill();
                });
            });

            // Draw legend
            const legendX = padding.left;
            let legendY = 20;
            const legendItemHeight = 20;

            ctx.textAlign = 'left';
            ctx.textBaseline = 'middle';
            ctx.font = '14px sans-serif';

            optimizedDatasets.forEach((dataset, index) => {
                const color = colors[index % colors.length];

                // Draw color box
                ctx.fillStyle = color;
                ctx.fillRect(legendX, legendY - 6, 12, 12);

                // Draw label
                ctx.fillStyle = textColor;
                ctx.fillText(dataset.label, legendX + 20, legendY);

                legendY += legendItemHeight;
            });

            console.log('[Chart] Chart rendered successfully');
            return true;
        } catch (error) {
            console.error('[Chart] Error creating chart:', error);
            return false;
        }
    },

    handleMouseMove: function(e, chartInfo) {
        const rect = chartInfo.canvas.getBoundingClientRect();
        const mouseX = e.clientX - rect.left;
        const mouseY = e.clientY - rect.top;

        // Find closest data point
        let closestPoint = null;
        let minDistance = 15; // Maximum distance to trigger tooltip

        chartInfo.datasets.forEach((dataset, datasetIndex) => {
            dataset.indices.forEach((originalIndex, i) => {
                const x = chartInfo.getX(originalIndex);
                const y = chartInfo.getY(dataset.data[i]);
                const distance = Math.sqrt(Math.pow(mouseX - x, 2) + Math.pow(mouseY - y, 2));

                if (distance < minDistance) {
                    minDistance = distance;
                    closestPoint = {
                        datasetIndex,
                        index: originalIndex,
                        value: dataset.data[i],
                        label: chartInfo.labels[originalIndex],
                        datasetLabel: dataset.label,
                        x,
                        y
                    };
                }
            });
        });

        if (closestPoint !== chartInfo.tooltip?.point) {
            chartInfo.tooltip = closestPoint ? { point: closestPoint } : null;
            this.redrawChart(chartInfo);
        }
    },

    handleMouseLeave: function(chartInfo) {
        if (chartInfo.tooltip) {
            chartInfo.tooltip = null;
            this.redrawChart(chartInfo);
        }
    },

    redrawChart: function(chartInfo) {
        const { ctx, width, height, padding, chartWidth, chartHeight, minValue, maxValue,
                getX, getY, colors, lineWidth, datasets, labels, tooltip } = chartInfo;

        // Detect theme
        const isDarkTheme = window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches;
        const textColor = isDarkTheme ? '#e5e7eb' : '#1f2937';
        const secondaryTextColor = isDarkTheme ? '#9ca3af' : '#6b7280';
        const gridColor = isDarkTheme ? '#374151' : '#e5e7eb';
        const axisColor = isDarkTheme ? '#9ca3af' : '#1f2937';

        // Clear and redraw
        ctx.clearRect(0, 0, width, height);

        // Draw grid lines
        ctx.strokeStyle = gridColor;
        ctx.lineWidth = 1;
        ctx.setLineDash([5, 5]);

        const gridLines = 5;
        for (let i = 0; i <= gridLines; i++) {
            const y = padding.top + (i / gridLines) * chartHeight;
            ctx.beginPath();
            ctx.moveTo(padding.left, y);
            ctx.lineTo(padding.left + chartWidth, y);
            ctx.stroke();
        }
        ctx.setLineDash([]);

        // Draw axes
        ctx.strokeStyle = axisColor;
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.moveTo(padding.left, padding.top);
        ctx.lineTo(padding.left, padding.top + chartHeight);
        ctx.lineTo(padding.left + chartWidth, padding.top + chartHeight);
        ctx.stroke();

        // Draw Y-axis labels
        ctx.fillStyle = secondaryTextColor;
        ctx.font = '12px sans-serif';
        ctx.textAlign = 'right';
        ctx.textBaseline = 'middle';

        for (let i = 0; i <= gridLines; i++) {
            const value = maxValue - (i / gridLines) * (maxValue - minValue);
            const y = padding.top + (i / gridLines) * chartHeight;
            ctx.fillText(value.toFixed(2), padding.left - 10, y);
        }

        // Draw X-axis labels
        ctx.textAlign = 'center';
        ctx.textBaseline = 'top';
        const minLabelSpacing = 60;
        const maxLabels = Math.floor(chartWidth / minLabelSpacing);
        const labelStep = Math.max(1, Math.ceil(labels.length / maxLabels));

        labels.forEach((label, index) => {
            if (label && index % labelStep === 0) {
                const x = getX(index);
                ctx.fillText(label, x, padding.top + chartHeight + 10);
            }
        });

        // Draw datasets
        datasets.forEach((dataset, datasetIndex) => {
            const color = colors[datasetIndex % colors.length];
            ctx.strokeStyle = color;
            ctx.lineWidth = lineWidth;
            ctx.beginPath();

            dataset.indices.forEach((originalIndex, i) => {
                const x = getX(originalIndex);
                const y = getY(dataset.data[i]);
                if (i === 0) ctx.moveTo(x, y);
                else ctx.lineTo(x, y);
            });
            ctx.stroke();

            // Draw points
            dataset.indices.forEach((originalIndex, i) => {
                const x = getX(originalIndex);
                const y = getY(dataset.data[i]);
                ctx.fillStyle = color;
                ctx.beginPath();
                ctx.arc(x, y, 3, 0, 2 * Math.PI);
                ctx.fill();
            });
        });

        // Draw legend
        const legendX = padding.left;
        let legendY = 20;
        ctx.textAlign = 'left';
        ctx.textBaseline = 'middle';
        ctx.font = '14px sans-serif';

        datasets.forEach((dataset, index) => {
            const color = colors[index % colors.length];
            ctx.fillStyle = color;
            ctx.fillRect(legendX, legendY - 6, 12, 12);
            ctx.fillStyle = textColor;
            ctx.fillText(dataset.label, legendX + 20, legendY);
            legendY += 20;
        });

        // Draw tooltip if exists
        if (tooltip) {
            this.drawTooltip(chartInfo, tooltip);
        }
    },

    drawTooltip: function(chartInfo, tooltip) {
        const { ctx, colors } = chartInfo;
        const point = tooltip.point;

        // Highlight the point
        const color = colors[point.datasetIndex % colors.length];
        ctx.fillStyle = color;
        ctx.strokeStyle = '#fff';
        ctx.lineWidth = 2;
        ctx.beginPath();
        ctx.arc(point.x, point.y, 5, 0, 2 * Math.PI);
        ctx.fill();
        ctx.stroke();

        // Draw tooltip box
        const tooltipText = [
            point.datasetLabel,
            point.label,
            `Value: ${point.value.toFixed(2)}`
        ];

        ctx.font = '12px sans-serif';
        const tooltipPadding = 8;
        const lineHeight = 16;
        const tooltipWidth = Math.max(...tooltipText.map(t => ctx.measureText(t).width)) + tooltipPadding * 2;
        const tooltipHeight = tooltipText.length * lineHeight + tooltipPadding * 2;

        // Position tooltip (avoid edges)
        let tooltipX = point.x + 10;
        let tooltipY = point.y - tooltipHeight - 10;

        if (tooltipX + tooltipWidth > chartInfo.width - 10) {
            tooltipX = point.x - tooltipWidth - 10;
        }
        if (tooltipY < 10) {
            tooltipY = point.y + 10;
        }

        // Draw tooltip background
        ctx.fillStyle = 'rgba(0, 0, 0, 0.8)';
        ctx.fillRect(tooltipX, tooltipY, tooltipWidth, tooltipHeight);

        // Draw tooltip text
        ctx.fillStyle = '#fff';
        ctx.textAlign = 'left';
        ctx.textBaseline = 'top';
        tooltipText.forEach((text, i) => {
            ctx.fillText(text, tooltipX + tooltipPadding, tooltipY + tooltipPadding + i * lineHeight);
        });
    },

    destroyChart: function(canvas) {
        if (!canvas) {
            return;
        }

        const chartInfo = this.charts.get(canvas);
        if (chartInfo) {
            if (chartInfo.canvas && chartInfo.canvas.getContext) {
                const ctx = chartInfo.canvas.getContext('2d');
                if (ctx) {
                    ctx.clearRect(0, 0, chartInfo.canvas.width, chartInfo.canvas.height);
                }
            }
            this.charts.delete(canvas);
        }
    }
};
