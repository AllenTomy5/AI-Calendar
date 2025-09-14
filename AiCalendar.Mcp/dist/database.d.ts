export interface CalendarEvent {
    id?: number;
    title: string;
    start: string;
    end: string;
    timezone?: string;
    location?: string;
    attendees?: string[];
    notes?: string;
    client_reference_id?: string;
    created_at?: string;
    updated_at?: string;
}
export declare class CalendarDatabase {
    private db;
    initialize(): Promise<void>;
    private createTables;
    saveEvent(event: CalendarEvent): Promise<CalendarEvent>;
    updateEventById(id: number, updates: Partial<CalendarEvent>): Promise<CalendarEvent>;
    deleteEvent(id: number): Promise<boolean>;
    deleteByClientReferenceId(clientReferenceId: string): Promise<boolean>;
    findById(id: number): Promise<CalendarEvent | null>;
    findByClientReferenceId(clientReferenceId: string): Promise<CalendarEvent | null>;
    listEvents(startDate?: string, endDate?: string, limit?: number): Promise<CalendarEvent[]>;
    private mapRowToEvent;
}
//# sourceMappingURL=database.d.ts.map