import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../environments/environment';

export interface MonthlyRevenue {
  month: string;
  orderCount: number;
  uniqueCustomers: number;
  revenue: number;
  avgOrderValue: number;
}

export interface TopProduct {
  productId: number;
  name: string;
  category: string;
  unitsSold: number;
  revenue: number;
}

export interface CustomerGrowth {
  month: string;
  newCustomers: number;
  cumulativeCustomers: number;
}

export interface CategoryRevenue {
  category: string;
  revenue: number;
  unitsSold: number;
}

export interface OrderStatus {
  status: string;
  orderCount: number;
  totalValue: number;
}

export interface DashboardSummary {
  totalRevenue: number;
  totalOrders: number;
  totalCustomers: number;
  avgOrderValue: number;
  revenueChangePercent: number;
}

export interface AiInsightResponse {
  answer: string;
  chartJson?: string | null;
  error?: string | null;
}

@Injectable({ providedIn: 'root' })
export class AnalyticsService {
  private readonly http = inject(HttpClient);
  private readonly apiBase = environment.apiBase;

  getSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.apiBase}/summary`);
  }

  getMonthlyRevenue(): Observable<MonthlyRevenue[]> {
    return this.http.get<MonthlyRevenue[]>(`${this.apiBase}/monthly-revenue`);
  }

  getTopProducts(limit = 10): Observable<TopProduct[]> {
    return this.http.get<TopProduct[]>(`${this.apiBase}/top-products?limit=${limit}`);
  }

  getCustomerGrowth(): Observable<CustomerGrowth[]> {
    return this.http.get<CustomerGrowth[]>(`${this.apiBase}/customer-growth`);
  }

  getRevenueByCategory(): Observable<CategoryRevenue[]> {
    return this.http.get<CategoryRevenue[]>(`${this.apiBase}/revenue-by-category`);
  }

  getOrderStatus(): Observable<OrderStatus[]> {
    return this.http.get<OrderStatus[]>(`${this.apiBase}/order-status`);
  }

  askAi(question: string): Observable<AiInsightResponse> {
    return this.http.post<AiInsightResponse>(`${this.apiBase}/insights`, { question });
  }
}