export const REACTION_OPTIONS = ["👍", "❤️", "🔥", "😀", "👏"];

export function getSelectedReaction(reactions = []) {
  return reactions.find((reaction) => reaction.reactedByCurrentUser) ?? null;
}

export function getTopReactionSummary(reactions = []) {
  return [...reactions]
    .filter((reaction) => reaction.count > 0)
    .sort((left, right) => {
      if (right.count !== left.count) {
        return right.count - left.count;
      }

      return REACTION_OPTIONS.indexOf(left.emoji) - REACTION_OPTIONS.indexOf(right.emoji);
    })
    .slice(0, 3);
}

export function applyReactionUpdateToThreads(threads, commentId, nextEmoji) {
  return threads.map((thread) => ({
    ...thread,
    comment: updateCommentReaction(thread.comment, commentId, nextEmoji),
    replies: thread.replies?.map((reply) => updateCommentReaction(reply, commentId, nextEmoji)) ?? []
  }));
}

function updateCommentReaction(comment, commentId, nextEmoji) {
  if (comment.id !== commentId) {
    return comment;
  }

  const currentReaction = getSelectedReaction(comment.reactions);
  const currentEmoji = currentReaction?.emoji ?? null;

  let nextReactions = [...(comment.reactions ?? [])];

  if (currentEmoji) {
    nextReactions = nextReactions
      .map((reaction) =>
        reaction.emoji === currentEmoji
          ? {
              ...reaction,
              count: Math.max(0, reaction.count - 1),
              reactedByCurrentUser: false
            }
          : reaction
      )
      .filter((reaction) => reaction.count > 0 || reaction.reactedByCurrentUser);
  }

  if (nextEmoji && nextEmoji !== currentEmoji) {
    const existingIndex = nextReactions.findIndex((reaction) => reaction.emoji === nextEmoji);
    if (existingIndex >= 0) {
      nextReactions[existingIndex] = {
        ...nextReactions[existingIndex],
        count: nextReactions[existingIndex].count + 1,
        reactedByCurrentUser: true
      };
    } else {
      nextReactions.push({
        emoji: nextEmoji,
        count: 1,
        reactedByCurrentUser: true
      });
    }
  }

  nextReactions = nextReactions
    .filter((reaction) => reaction.count > 0 || reaction.reactedByCurrentUser)
    .sort((left, right) => {
      if (right.count !== left.count) {
        return right.count - left.count;
      }

      return REACTION_OPTIONS.indexOf(left.emoji) - REACTION_OPTIONS.indexOf(right.emoji);
    });

  return {
    ...comment,
    reactions: nextReactions
  };
}
