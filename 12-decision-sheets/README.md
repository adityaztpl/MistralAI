# Decision Sheets

Short, interview-ready decision sheets for full-stack and GenAI architecture choices. Each sheet helps you answer: "What would you use, when, and why?"

## How to use these sheets

1. Read the comparison table first.
2. Memorize the default answer script.
3. Practice adapting the answer to a scenario: team skill set, existing codebase, latency, scale, security, compliance, delivery timeline, and maintenance burden.
4. End every interview answer with a trade-off, not a slogan.

## Files

| Sheet | Decision |
|---|---|
| [Angular vs React](angular-vs-react.md) | Frontend framework choice |
| [Controllers vs Minimal APIs](controllers-vs-minimal-apis.md) | ASP.NET Core HTTP API style |
| [LangChain vs LangGraph vs Semantic Kernel](langchain-vs-langgraph-vs-semantic-kernel.md) | LLM orchestration framework |
| [RAG vs Fine-tune vs Tools](rag-vs-finetune-vs-tools.md) | How an LLM should use external knowledge/actions |
| [REST vs gRPC vs SignalR](rest-vs-grpc-vs-signalr.md) | API communication style |
| [Zustand vs Redux vs React Query](zustand-vs-redux-vs-react-query.md) | React state and server cache choice |
| [Signals vs RxJS](signals-vs-rxjs.md) | Angular reactivity choice |
| [SQL vs Vector vs Hybrid Search](sql-vs-vector-vs-hybrid-search.md) | Search and retrieval architecture |

## Universal decision answer template

```text
I would start from the product requirement and constraints, not from the tool name.
For this case, the main forces are <latency/scale/team/history/compliance>.
My default choice is <option> because <primary reason>.
I would avoid <other option> unless <condition>.
The main trade-off is <cost/complexity/flexibility>.
I would validate the decision with <prototype/metrics/tests> before standardizing it.
```

## Interview scoring signals

Strong answers:

- Name the forces that matter.
- Pick a default and defend it.
- Mention what would change the decision.
- Include migration and operational concerns.
- Discuss tests, metrics, and failure modes.

Weak answers:

- Say one option is "always better".
- Choose based only on popularity.
- Ignore the existing team and codebase.
- Forget security, observability, or cost.
- Compare syntax instead of architecture.
