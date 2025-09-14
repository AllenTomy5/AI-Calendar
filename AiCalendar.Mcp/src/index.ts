import { Server } from '@modelcontextprotocol/sdk/server/index.js';
import { StdioServerTransport } from '@modelcontextprotocol/sdk/server/stdio.js';
import {
  CallToolRequestSchema,
  ErrorCode,
  ListToolsRequestSchema,
  McpError,
} from '@modelcontextprotocol/sdk/types.js';
import express from 'express';
import cors from 'cors';
import { CalendarDatabase } from './database.js';
import { CalendarTools } from './tools.js';

class CalendarMcpServer {
  private server: Server;
  private database: CalendarDatabase;
  private tools: CalendarTools;

  constructor() {
    this.server = new Server(
      {
        name: 'aicalendar-mcp-server',
        version: '1.0.0',
      },
      {
        capabilities: {
          tools: {},
        },
      }
    );

    this.database = new CalendarDatabase();
    this.tools = new CalendarTools(this.database);
    this.setupToolHandlers();
  }

  private setupToolHandlers() {
    this.server.setRequestHandler(ListToolsRequestSchema, async () => {
      return {
        tools: [
          {
            name: 'calendar.save_event',
            description: 'Save or update a calendar event with idempotency via client_reference_id',
            inputSchema: {
              type: 'object',
              properties: {
                title: { type: 'string', description: 'Event title (required)' },
                start: { type: 'string', format: 'date-time', description: 'Start time (required)' },
                end: { type: 'string', format: 'date-time', description: 'End time (required)' },
                timezone: { type: 'string', description: 'Timezone (optional, defaults to UTC)' },
                location: { type: 'string', description: 'Event location (optional)' },
                attendees: { 
                  type: 'array', 
                  items: { type: 'string' },
                  description: 'List of attendee email addresses (optional)'
                },
                notes: { type: 'string', description: 'Additional notes (optional)' },
                client_reference_id: { type: 'string', description: 'Client reference ID for idempotency (optional)' }
              },
              required: ['title', 'start', 'end']
            }
          },
          {
            name: 'calendar.update_event',
            description: 'Update an existing calendar event',
            inputSchema: {
              type: 'object',
              properties: {
                id: { type: 'number', description: 'Event ID' },
                client_reference_id: { type: 'string', description: 'Client reference ID' },
                title: { type: 'string', description: 'Event title' },
                start: { type: 'string', format: 'date-time', description: 'Start time' },
                end: { type: 'string', format: 'date-time', description: 'End time' },
                timezone: { type: 'string', description: 'Timezone' },
                location: { type: 'string', description: 'Event location' },
                attendees: { 
                  type: 'array', 
                  items: { type: 'string' },
                  description: 'List of attendee email addresses'
                },
                notes: { type: 'string', description: 'Additional notes' }
              }
            }
          },
          {
            name: 'calendar.cancel_event',
            description: 'Cancel (delete) a calendar event',
            inputSchema: {
              type: 'object',
              properties: {
                id: { type: 'number', description: 'Event ID' },
                client_reference_id: { type: 'string', description: 'Client reference ID' }
              }
            }
          },
          {
            name: 'calendar.list_events',
            description: 'List calendar events with optional filtering',
            inputSchema: {
              type: 'object',
              properties: {
                start_date: { type: 'string', format: 'date-time', description: 'Filter events from this date' },
                end_date: { type: 'string', format: 'date-time', description: 'Filter events until this date' },
                limit: { type: 'number', description: 'Maximum number of events to return' }
              }
            }
          }
        ],
      };
    });

    this.server.setRequestHandler(CallToolRequestSchema, async (request) => {
      try {
        switch (request.params.name) {
          case 'calendar.save_event':
            return await this.tools.saveEvent(request.params.arguments);
          
          case 'calendar.update_event':
            return await this.tools.updateEvent(request.params.arguments);
          
          case 'calendar.cancel_event':
            return await this.tools.cancelEvent(request.params.arguments);
          
          case 'calendar.list_events':
            return await this.tools.listEvents(request.params.arguments);
          
          default:
            throw new McpError(
              ErrorCode.MethodNotFound,
              `Unknown tool: ${request.params.name}`
            );
        }
      } catch (error) {
        const errorMessage = error instanceof Error ? error.message : 'Unknown error';
        throw new McpError(ErrorCode.InternalError, errorMessage);
      }
    });
  }

  async run() {
    await this.database.initialize();
    
    // Start stdio server
    const transport = new StdioServerTransport();
    await this.server.connect(transport);
    console.error('AI Calendar MCP server running on stdio');
    
    // Also start HTTP server for API compatibility
    this.startHttpServer();
  }

  private startHttpServer() {
    const app = express();
    app.use(cors());
    app.use(express.json());

    // Health check endpoint
    app.get('/health', (req, res) => {
      res.json({ status: 'ok', server: 'AI Calendar MCP Server' });
    });

    // List available tools
    app.get('/tools', async (req, res) => {
      try {
        const result = await this.server.request(
          { method: 'tools/list' },
          ListToolsRequestSchema
        );
        res.json(result);
      } catch (error) {
        res.status(500).json({ error: error instanceof Error ? error.message : 'Unknown error' });
      }
    });

    // Call a specific tool
    app.post('/tools/:toolName', async (req, res) => {
      try {
        const { toolName } = req.params;
        const toolArguments = req.body;

        const result = await this.server.request(
          {
            method: 'tools/call',
            params: {
              name: toolName,
              arguments: toolArguments
            }
          },
          CallToolRequestSchema
        );
        
        res.json(result);
      } catch (error) {
        console.error('Tool call error:', error);
        res.status(500).json({ 
          error: error instanceof Error ? error.message : 'Unknown error',
          toolName: req.params.toolName,
          arguments: req.body
        });
      }
    });

    const port = process.env.PORT || 3000;
    app.listen(port, () => {
      console.error(`HTTP server running on port ${port}`);
    });
  }
}

const server = new CalendarMcpServer();
server.run().catch(console.error);