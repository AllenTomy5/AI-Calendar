import { CalendarDatabase } from './database.js';
export declare class CalendarTools {
    private database;
    constructor(database: CalendarDatabase);
    saveEvent(args: any): Promise<{
        content: {
            type: string;
            text: string;
        }[];
    }>;
    updateEvent(args: any): Promise<{
        content: {
            type: string;
            text: string;
        }[];
    }>;
    cancelEvent(args: any): Promise<{
        content: {
            type: string;
            text: string;
        }[];
    }>;
    listEvents(args: any): Promise<{
        content: {
            type: string;
            text: string;
        }[];
    }>;
}
//# sourceMappingURL=tools.d.ts.map