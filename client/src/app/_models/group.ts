export interface Group {
  name: String;
  connections: Connection[];
}

interface Connection {
  connectionId: string;
  username: string;
}
