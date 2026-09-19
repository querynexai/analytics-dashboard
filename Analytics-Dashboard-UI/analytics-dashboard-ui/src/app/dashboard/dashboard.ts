import { Component, OnInit, signal, inject, ChangeDetectionStrategy } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { NgxEchartsDirective, provideEchartsCore } from 'ngx-echarts';
import * as echarts from 'echarts/core';
import { LineChart, BarChart, PieChart } from 'echarts/charts';
import {
  GridComponent, TooltipComponent, LegendComponent,
  TitleComponent, DataZoomComponent
} from 'echarts/components';
import { CanvasRenderer } from 'echarts/renderers';
import {
  AnalyticsService, DashboardSummary, MonthlyRevenue,
  TopProduct, CustomerGrowth, CategoryRevenue, OrderStatus
} from '../analytics.service';

echarts.use([
  LineChart, BarChart, PieChart,
  GridComponent, TooltipComponent, LegendComponent,
  TitleComponent, DataZoomComponent, CanvasRenderer
]);

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule, FormsModule, NgxEchartsDirective],
  providers: [provideEchartsCore({ echarts })],
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dashboard.html',
  styleUrls: ['./dashboard.css']
})
export class DashboardComponent implements OnInit {
  private readonly api = inject(AnalyticsService);

  readonly summary = signal<DashboardSummary | null>(null);

  readonly revenueChart = signal<any>(null);
  readonly topProductsChart = signal<any>(null);
  readonly customerGrowthChart = signal<any>(null);
  readonly categoryChart = signal<any>(null);
  readonly orderStatusChart = signal<any>(null);

  aiQuestion = '';
  readonly aiAnswer = signal('');
  readonly aiLoading = signal(false);
  readonly aiError = signal('');

  readonly loading = signal(true);

  readonly quickQuestions = [
    'How has revenue trended month over month?',
    'Which products drive the most revenue?',
    'How is customer growth looking?',
    'Which category performs best?',
    'What is the current order status breakdown?',
    'Give me an executive summary of the business.'
  ];

  ngOnInit(): void {
    this.loadAll();
  }

  private loadAll(): void {
    this.loading.set(true);

    this.api.getSummary().subscribe({
      next: (s) => this.summary.set(s)
    });

    this.api.getMonthlyRevenue().subscribe({
      next: (data) => this.revenueChart.set(this.buildRevenueChart(data))
    });

    this.api.getTopProducts(10).subscribe({
      next: (data) => this.topProductsChart.set(this.buildTopProductsChart(data))
    });

    this.api.getCustomerGrowth().subscribe({
      next: (data) => this.customerGrowthChart.set(this.buildCustomerGrowthChart(data))
    });

    this.api.getRevenueByCategory().subscribe({
      next: (data) => this.categoryChart.set(this.buildCategoryChart(data))
    });

    this.api.getOrderStatus().subscribe({
      next: (data) => {
        this.orderStatusChart.set(this.buildOrderStatusChart(data));
        this.loading.set(false);
      }
    });
  }

  askAi(): void {
    const q = this.aiQuestion.trim();
    if (!q) return;

    this.aiLoading.set(true);
    this.aiError.set('');
    this.aiAnswer.set('');

    this.api.askAi(q).subscribe({
      next: (res) => {
        this.aiLoading.set(false);
        if (res.error) {
          this.aiError.set(res.error);
        } else {
          this.aiAnswer.set(res.answer);
        }
      },
      error: () => {
        this.aiLoading.set(false);
        this.aiError.set('Failed to reach the AI service.');
      }
    });
  }

  useQuickQuestion(q: string): void {
    this.aiQuestion = q;
    this.askAi();
  }

  private buildRevenueChart(data: MonthlyRevenue[]): any {
    return {
      tooltip: { trigger: 'axis' },
      legend: { data: ['Revenue', 'Orders'], textStyle: { color: '#ccc' } },
      grid: { left: 60, right: 30, top: 40, bottom: 40 },
      xAxis: {
        type: 'category',
        data: data.map(d => new Date(d.month).toLocaleDateString('en', { month: 'short', year: '2-digit' })),
        axisLabel: { color: '#888' }
      },
      yAxis: [
        { type: 'value', name: 'Revenue', axisLabel: { color: '#888' }, splitLine: { lineStyle: { color: '#2a2a40' } } },
        { type: 'value', name: 'Orders', axisLabel: { color: '#888' }, splitLine: { show: false } }
      ],
      series: [
        {
          name: 'Revenue',
          type: 'line',
          smooth: true,
          areaStyle: { opacity: 0.15 },
          data: data.map(d => d.revenue),
          itemStyle: { color: '#00b4d8' }
        },
        {
          name: 'Orders',
          type: 'bar',
          yAxisIndex: 1,
          data: data.map(d => d.orderCount),
          itemStyle: { color: '#48cae4', opacity: 0.6 }
        }
      ]
    };
  }

  private buildTopProductsChart(data: TopProduct[]): any {
    const top = [...data].reverse();
    return {
      tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
      grid: { left: 160, right: 30, top: 20, bottom: 30 },
      xAxis: { type: 'value', axisLabel: { color: '#888' }, splitLine: { lineStyle: { color: '#2a2a40' } } },
      yAxis: {
        type: 'category',
        data: top.map(d => d.name.length > 20 ? d.name.slice(0, 18) + '…' : d.name),
        axisLabel: { color: '#ccc', fontSize: 11 }
      },
      series: [{
        type: 'bar',
        data: top.map(d => d.revenue),
        itemStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
            { offset: 0, color: '#0096c7' },
            { offset: 1, color: '#48cae4' }
          ]),
          borderRadius: [0, 4, 4, 0]
        }
      }]
    };
  }

  private buildCustomerGrowthChart(data: CustomerGrowth[]): any {
    return {
      tooltip: { trigger: 'axis' },
      legend: { data: ['New Customers', 'Cumulative'], textStyle: { color: '#ccc' } },
      grid: { left: 50, right: 30, top: 40, bottom: 40 },
      xAxis: {
        type: 'category',
        data: data.map(d => new Date(d.month).toLocaleDateString('en', { month: 'short', year: '2-digit' })),
        axisLabel: { color: '#888' }
      },
      yAxis: { type: 'value', axisLabel: { color: '#888' }, splitLine: { lineStyle: { color: '#2a2a40' } } },
      series: [
        {
          name: 'New Customers',
          type: 'bar',
          data: data.map(d => d.newCustomers),
          itemStyle: { color: '#48cae4' }
        },
        {
          name: 'Cumulative',
          type: 'line',
          smooth: true,
          data: data.map(d => d.cumulativeCustomers),
          itemStyle: { color: '#00b4d8' }
        }
      ]
    };
  }

  private buildCategoryChart(data: CategoryRevenue[]): any {
    return {
      tooltip: { trigger: 'item', formatter: '{b}: {c} ({d}%)' },
      legend: { orient: 'vertical', right: 10, textStyle: { color: '#ccc' } },
      series: [{
        type: 'pie',
        radius: ['40%', '70%'],
        center: ['40%', '50%'],
        data: data.map(d => ({ name: d.category, value: d.revenue })),
        label: { color: '#ccc' },
        itemStyle: { borderColor: '#1a1a2e', borderWidth: 2 }
      }]
    };
  }

  private buildOrderStatusChart(data: OrderStatus[]): any {
    return {
      tooltip: { trigger: 'item' },
      legend: { bottom: 0, textStyle: { color: '#ccc' } },
      series: [{
        type: 'pie',
        radius: ['45%', '70%'],
        data: data.map(d => ({ name: d.status, value: d.orderCount })),
        label: { color: '#ccc' },
        itemStyle: { borderColor: '#1a1a2e', borderWidth: 2 }
      }]
    };
  }

  formatCurrency(value: number): string {
    return new Intl.NumberFormat('en-IN', { style: 'currency', currency: 'INR', maximumFractionDigits: 0 }).format(value);
  }
}